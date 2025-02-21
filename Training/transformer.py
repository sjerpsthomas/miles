import torch
import torch.nn as nn

# DEFINE MODEL PARAMETERS

VOCAB_SIZE = 16
D_MODEL = 32
NUM_HEADS = 4
NUM_LAYERS = 3
FF_DIM = 64
SEQ_LEN = 10

# DEFINE MODEL

class DecoderOnlyTransformer(nn.Module):
    def __init__(self):
        super().__init__()
        
        # Get input embedding and positional embedding
        self.input_embedding = nn.Embedding(VOCAB_SIZE, D_MODEL)
        self.positional_embedding = nn.Embedding(SEQ_LEN, D_MODEL)

        # Create decoder layers
        self.layers = nn.ModuleList([
            nn.TransformerEncoderLayer(D_MODEL, NUM_HEADS, FF_DIM, dropout=0.1, batch_first=True)
            for _ in range(NUM_LAYERS)
        ])

        # Add linear layer that generates logits
        self.linear = nn.Linear(D_MODEL, VOCAB_SIZE)

    def forward(self, x: torch.Tensor):
        _, seq_len = x.shape

        # Get all positions (integer values)
        positions: torch.Tensor = torch.arange(seq_len, device=x.device).unsqueeze(0)
        
        # Add input embedding
        x = self.input_embedding(x) + self.positional_embedding(positions)

        # Generate causal mask (prevents future token access)
        mask: torch.Tensor = torch.triu(torch.ones(seq_len, seq_len), diagonal=1).to(x.device)
        mask = mask.masked_fill(mask == 1, float('-inf'))

        # Evaluate layers
        for layer in self.layers:
            x = layer(x, mask)

        # Get logits, return
        logits = self.linear(x) 
        return logits
