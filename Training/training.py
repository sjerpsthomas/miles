import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import DataLoader, TensorDataset
from transformer import *


# (trains the model with the specified paramters)
def train(
    model: DecoderOnlyTransformer,
    num_epochs: int,
    learning_rate: float,
    batch_size: int,
    dataset: TensorDataset,
    save_location: str,
) -> None:
    data_loader: DataLoader = DataLoader(dataset, batch_size=batch_size, shuffle=True)

    # Create device
    device: torch.device = torch.device("cuda" if torch.cuda.is_available() else "cpu")

    # create model, loss and optimizer
    model = model.to(device)
    loss_fn: nn.CrossEntropyLoss = nn.CrossEntropyLoss()
    optimizer: optim.Adam = optim.Adam(model.parameters(), lr=learning_rate)

    # Train
    for epoch in range(num_epochs):
        model.train()
        total_loss: float = 0

        for inputs, targets in data_loader:
            inputs, targets = inputs.to(device), targets.to(device)

            # Evaluate the model
            optimizer.zero_grad()
            logits: torch.Tensor = model(inputs)

            # Reshape for loss computation: (batch*seq_len, vocab_size)
            #  (whatever that may mean)
            loss: torch.Tensor = loss_fn(logits.view(-1, VOCAB_SIZE), targets.view(-1))
            loss.backward()
            optimizer.step()
            
            total_loss += loss.item()

        avg_loss = total_loss / len(data_loader)
        print(f"[PT] Epoch {epoch+1}/{num_epochs}, Loss: {avg_loss:.4f}")


    # Save the model
    torch.save(model.state_dict(), save_location)