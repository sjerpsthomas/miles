import torch
from torch.utils.data import TensorDataset
from transformer import *
import training


# Hyperparameters
NUM_EPOCHS = 1
LEARNING_RATE = 0.001
BATCH_SIZE = 32


def pre_train(
    dataset_file_path: str
) -> None:
    # Print
    print("[pre_train] Pre-training started!")

    # Create model
    model: DecoderOnlyTransformer = DecoderOnlyTransformer()

    # Train    
    training.train(
        given_model=model,
        dataset_file_path=dataset_file_path,
        num_epochs=NUM_EPOCHS,
        learning_rate=LEARNING_RATE,
        batch_size=BATCH_SIZE,
        save_location="pretrained_transformer.pth"
    )

    # Print
    print("[pre_train] Pre-training ended!")
