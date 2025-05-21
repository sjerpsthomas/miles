

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