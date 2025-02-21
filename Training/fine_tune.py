import torch
from torch.utils.data import TensorDataset
from transformer import *
import training


# Hyperparameters
NUM_EPOCHS = 3
LEARNING_RATE = 0.0001
BATCH_SIZE = 32


def fine_tune(
    dataset_file_path: str
) -> None:
    # Print
    print("[fine_tune] Fine-tuning started!")

    # Create model
    model: DecoderOnlyTransformer = DecoderOnlyTransformer()
    model.load_state_dict(torch.load("pretrained_transformer.pth"))

    # Train
    training.train(
        given_model=model,
        dataset_file_path=dataset_file_path,
        num_epochs=NUM_EPOCHS,
        learning_rate=LEARNING_RATE,
        batch_size=BATCH_SIZE,
        save_location="finetuned_transformer.pth"
    )

    # Print
    print("[fine_tune] Fine-tuning ended!")
