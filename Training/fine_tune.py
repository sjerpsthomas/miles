import torch
from torch.utils.data import TensorDataset
from transformer import *
import training


# Hyperparameters
NUM_EPOCHS = 3
LEARNING_RATE = 0.0001
BATCH_SIZE = 32


def fine_tune() -> None:
    # Create model
    model: DecoderOnlyTransformer = DecoderOnlyTransformer()
    model.load_state_dict(torch.load("pretrained_transformer.pth"))

    # (generates the data)
    def generate_data(batch_size, seq_len, vocab_size) -> TensorDataset:
        # TODO: load data
        inputs = torch.randint(0, vocab_size, (batch_size, seq_len))
        
        targets = inputs.clone()  # For autoregressive training, next token is target
        
        return inputs, targets

    # Create dataset
    dataset: TensorDataset = generate_data(2000, 10, 16)

    # Train
    training.train(
        model = model,
        num_epochs=NUM_EPOCHS,
        learning_rate=LEARNING_RATE,
        batch_size=BATCH_SIZE,
        dataset=dataset,
        save_location="finetuned_transformer.pth"
    )