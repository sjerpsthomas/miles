import sys
import torch
import torch.nn as nn
import torch.onnx

# Import transformer.py from parent directory
sys.path.append("../")
from transformer import *

# Get command line arguments
file_path: str = sys.argv[1]

# Instantiate model and set to eval mode
model: DecoderOnlyTransformer = DecoderOnlyTransformer()
model.load_state_dict(torch.load(file_path))
model.eval()

# Create a dummy input tensor (batch_size=1, seq_len=SEQ_LEN)
dummy_input: torch.Tensor = torch.randint(0, VOCAB_SIZE, (1, SEQ_LEN))

# Export model to ONNX
onnx_program = torch.onnx.export(
    model, 
    dummy_input, 
    input_names=["input"],
    output_names=["output"],
    dynamic_axes={"input": {0: "batch_size"}, "output": {0: "batch_size"}},
    opset_version=14,
    dynamo=True,
)

onnx_program.optimize()
onnx_program.save("onnx_transformer.onnx")

# Print
print(f"Model exported!")
