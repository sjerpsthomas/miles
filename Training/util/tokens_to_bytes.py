import sys

# Import tokens.py from parent directory
sys.path.append("../")
from tokens import *

path_in: str = sys.argv[1]
path_out: str = sys.argv[2]

with open(path_in, "r") as file:
    print("opened file")
    # Get file contents
    token_chars: str = file.read()
    
    print("read file")
    tokens = (token_map[c].to_bytes() for c in token_chars)
    with open(path_out, "wb") as file:
        file.write(b''.join(tokens))