import os

moves = {}

pokeNumber = 200
for i in range(pokeNumber):
    filePath = str(i) + ".txt"
    
    # check if the file exists
    if os.path.isfile(filePath):
        inMoves = False
        print("file opened")
        file = open(filePath, "r")
        
        lines = file.read().split("\n")
        for line in lines:
            stuff = line.split(":")
            handle = stuff[0]
            
            if inMoves:
                contentSplit = handle.split("/")
                moveName = contentSplit[0]
                if moveName not in moves:
                    moves[moveName] = 0
            
            if handle == "Moves":
                inMoves = True
        
        file.close()

for move in moves:
    print(move)
          