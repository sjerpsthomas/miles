import json
from util import CompactJSONEncoder

with open("info.json") as f:
    info = json.load(f)


form_config = [
    {
        0: [
            "ex2_par2_per3",
            "ex2_par1_per2",
            "ex2_par1_per0",
            "ex2_par2_per0",
        ],
        1: [
            "ex2_par2_per3",
            "ex2_par0_per2",
            "ex2_par0_per0",
            "ex2_par2_per0",
        ],
        2: [
            "ex2_par1_per3",
            "ex2_par0_per2",
            "ex2_par0_per0",
            "ex2_par1_per0",
        ]
    },
    {
        0: [
            "ex2_par1_per1",
            "ex2_par2_per1",
            "ex2_par2_per2",
            "ex2_par1_per3",
        ],
        1: [
            "ex2_par0_per1",
            "ex2_par2_per1",
            "ex2_par2_per2",
            "ex2_par0_per3",
        ],
        2: [
            "ex2_par0_per1",
            "ex2_par1_per1",
            "ex2_par1_per2",
            "ex2_par0_per3",
        ]
    }
]

data = [
    [[[1,2,2,1],[1,3,3,2],[1,3,3,2],[3,4,3,2]],[[2,2,2,2],[2,3,3,2],[4,3,1,2],[3,3,2,2]]],
    [[[3,3,2,2],[4,3,2,3],[3,3,2,3],[4,4,3,4]],[[1,2,1,1],[1,2,2,2],[1,2,1,2],[0,1,0,1]]],
    [[[2,3,1,2],[4,4,3,3],[4,4,2,2],[2,4,2,2]],[[1,2,2,2],[4,1,2,3],[1,3,2,2],[1,3,1,2]]],
]


for performance in info:
    pid = int(performance[7])

    info[performance]["scores"] = { pid: info[performance]["scores"] }


for i, config in enumerate(form_config):

    for expert_id in range(3):
        pids = config[expert_id]
        ratings = data[expert_id][i]

        pid: str
        rating: list[int]
        for pid, rating in zip(pids, ratings):
            print(pid, rating)

            info[pid]["scores"][expert_id] = rating


with open("info.json", "w") as f:
    json.dump(info, f, indent=4, cls=CompactJSONEncoder)