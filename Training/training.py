import torch
import torch.nn as nn
import torch.optim as optim
from torch.utils.data import DataLoader
from transformer import *
import tokens
from time import time

# (trains the model with the specified parameters)
def train(
    given_model: DecoderOnlyTransformer,
    dataset_file_path: str,
    num_epochs: int,
    learning_rate: float,
    batch_size: int,
    save_location: str,
) -> None:
    print("[training] Training started!")

    # Create device
    device_name: str = "cuda" if torch.cuda.is_available() else "cpu"
    device: torch.device = torch.device(device_name)
    print(f"[training] Using device {device_name}")

    # Initialize dataset
    dataset: tokens.TokenDataset = tokens.load_dataset(dataset_file_path)
    data_loader = DataLoader(
        dataset, batch_size=batch_size, shuffle=True, num_workers=8, 
        prefetch_factor=2, collate_fn=tokens.collate_fn, persistent_workers=True
    )

    # create model, loss and optimizer
    model = given_model.to(device)
    loss_fn: nn.CrossEntropyLoss = nn.CrossEntropyLoss()
    optimizer: optim.Adam = optim.Adam(model.parameters(), lr=learning_rate)

    start_time = time()

    progress_file = open("training_test_progress.txt", "a")

    # Train
    for epoch in range(num_epochs):
        model.train()

        total_loss: float = 0

        i: int = 0

        inputs: torch.Tensor
        targets: torch.Tensor
        for inputs, targets in data_loader:
            inputs, targets = inputs.to(device), targets.to(device)

            # Evaluate the model, evaluate loss
            optimizer.zero_grad()

            with torch.autocast(device_type="cuda", dtype=torch.float16):
                logits: torch.Tensor = model(inputs)

                loss: torch.Tensor = loss_fn(logits.view(-1, VOCAB_SIZE), targets.view(-1))
            
            loss.backward()
            optimizer.step()
            
            total_loss += loss.item()

            i += 1
            if i % 500 == 0:
                print(f"[training] Done with step {i}/{len(data_loader)}")
                progress_file.write(f"[training] Done with step {i}/{len(data_loader)}\n")
            
            if time() - start_time > 900:
                print(f"[training] Timing out")
                break

        avg_loss = total_loss / len(data_loader)
        print(f"[training] Epoch {epoch + 1}/{num_epochs}, Loss: {avg_loss:.4f}")

        torch.save(model.state_dict(), f"__intermediate_model_{epoch + 1}.pth")

        progress_file.write(f"[training] Done with epoch {epoch + 1}")


    progress_file.close()

    # Save the model
    torch.save(model.state_dict(), save_location)
    print(f"[training] Model saved to {save_location}")

    # Print time diff
    print(f"[training] Finished in {time() - start_time} seconds!")
