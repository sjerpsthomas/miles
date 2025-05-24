import json
from compact_json_encoder import CompactJSONEncoder

class Avg:
    x: float
    n: float

    def __init__(self):
        self.x = 0.0
        self.n = 0.0

    def add(self, add_x: float, add_n: float = 1.0):
        self.x += add_x
        self.n += add_n
    
    def avg(self): return self.x / self.n

    def __repr__(self): return "-" if self.n == 0.0 else f"{self.avg():.1f}"


def make_average_list(size: int) -> list[Avg]: return [Avg() for _ in range(size)]
def get_average(avgs: list[Avg]) -> list[float]: return [avg.avg() for avg in avgs]

with open("info.json") as f:
    info = json.load(f)




# [
#     [[2.8, 1.9], [2.5, 1.6], [3.1, 1.5], [3.2, -]],
#     [[3.3, 2.4], [3.2, 2.4], [3.0, 2.2], [3.5, -]],
#     [[3.5, 2.1], [2.7, 2.1], [3.4, 1.8], [3.2, -]],
#     [[3.9, 2.4], [3.2, 2.2], [3.6, 2.0], [4.0, -]]
# ]
