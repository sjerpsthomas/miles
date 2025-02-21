import sys

# Get command line arguments
file_path: str = sys.argv[1]
prefix: str = sys.argv[2]

with open(file_path, "r") as file:
    # Get file contents
    token_chars: str = file.read()
    length: int = len(token_chars)

    # Get indices of data sets
    #   (80% training set, 10% testing set, 10% validation set)
    train_end_index: int = int(0.8 * length)
    test_end_index: int  = int(0.9 * length)
    valid_end_index: int = length

    # Get slices
    train: str = token_chars[:train_end_index]
    test: str = token_chars[train_end_index + 1 : test_end_index]
    valid: str = token_chars[test_end_index + 1 : valid_end_index]

    # Save sliced strings to file
    with open(prefix + "train.tokens", "w") as train_file:
        train_file.write(train)
    with open(prefix + "test.tokens", "w") as test_file:
        test_file.write(test)
    with open(prefix + "valid.tokens", "w") as valid_file:
        valid_file.write(valid)

# Print
print("Tokens split successfully!")