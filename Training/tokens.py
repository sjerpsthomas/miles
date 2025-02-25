import transformer
import torch
from torch.utils.data import Dataset


class TokenDataset(Dataset):
    def __init__(self, tokens: torch.Tensor, seq_length: int):
        self.tokens = tokens
        self.seq_length = seq_length
        self.num_samples = len(tokens) - seq_length

    def __len__(self):
        return self.num_samples

    def __getitem__(self, idx):
        # Get token sequences
        input_seq: torch.Tensor = self.tokens[idx : idx + self.seq_length]
        target_seq: torch.Tensor = self.tokens[idx + 1 : idx + self.seq_length + 1]  # Shifted by 1 token

        # Convert to tensors
        return input_seq, target_seq


# (loads a token dataset from the specified file)
def load_dataset_bytes(file_path: str) -> TokenDataset:
    # Load tokens
    token_bytes: bytes
    with open(file_path, "rb") as file:
        token_bytes = file.read()
    
    # Load with NumPy, turn into tensor
    tokens_tensor = torch.frombuffer(token_bytes, dtype=torch.int8)
    tokens_tensor = tokens_tensor.to(torch.int64)

    # Create dataset
    dataset: TokenDataset = TokenDataset(tokens_tensor, transformer.SEQ_LEN)

    # Print status update
    print(f"[tokens] Loaded {len(dataset)} tokens in dataset")
    
    # return
    return dataset

