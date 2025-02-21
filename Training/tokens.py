from enum import IntEnum
import transformer
import torch
from torch.utils.data import Dataset

class Token(IntEnum): 
    Rest = 0

    Note1 = 1
    Note2 = 2
    Note3 = 3
    Note4 = 4
    Note5 = 5
    Note6 = 6
    Note7 = 7

    PassingTone = 8

    SuperFast = 9
    Fast = 10
    Slow = 11
    SuperSlow = 12

    Loud = 13
    Quiet = 14

    Measure = 15


# (converts token character into token integer)
token_map: dict[str, Token] = {
    '.': Token.Rest,
    '1': Token.Note1,
    '2': Token.Note2,
    '3': Token.Note3,
    '4': Token.Note4,
    '5': Token.Note5,
    '6': Token.Note6,
    '7': Token.Note7,
    'p': Token.PassingTone,
    'F': Token.SuperFast,
    'f': Token.Fast,
    's': Token.Slow,
    'S': Token.SuperSlow,
    'L': Token.Loud,
    'Q': Token.Quiet,
    'M': Token.Measure,
}


class TokenDataset(Dataset):
    def __init__(self, tokens: list[Token], seq_length: int):
        self.tokens = tokens
        self.seq_length = seq_length
        self.num_samples = len(tokens) - seq_length

    def __len__(self):
        return self.num_samples

    def __getitem__(self, idx):
        # Get token sequences
        input_seq: list[Token] = self.tokens[idx : idx + self.seq_length]
        target_seq: list[Token] = self.tokens[idx + 1 : idx + self.seq_length + 1]  # Shifted by 1 token

        # Convert to tensors
        inputs: torch.Tensor = torch.tensor(input_seq, dtype=torch.long)
        targets: torch.Tensor = torch.tensor(target_seq, dtype=torch.long)

        # Convert to tensors
        return inputs, targets


# (loads a token dataset from the specified file)
def load_dataset(file_path: str) -> TokenDataset:
    # Load tokens
    tokens: list[Token]
    with open(file_path, "r") as file:
        token_chars: str = file.read()
        tokens = list(token_map[c] for c in token_chars)

    # Create dataset
    dataset: TokenDataset = TokenDataset(tokens, transformer.SEQ_LEN)

    # Print status update
    print(f"[tokens] Loaded {len(dataset)} tokens in dataset")
    
    # return
    return dataset
