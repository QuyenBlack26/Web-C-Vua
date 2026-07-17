import chess

MATE_SCORE = 1_000_000
INF = 10**18

LEVELS = {
    "easy": {
        "max_depth": 1,
        "time_limit": 0.5,
        "random": 0.35,
    },
    "medium": {
        "max_depth": 3,
        "time_limit": 1.5,
        "random": 0.05,
    },
    "hard": {
        "max_depth": 4,
        "time_limit": 2.5,
        "random": 0.0,
    },
    "boss": {
        "max_depth": 5,
        "time_limit": 4.0,
        "random": 0.0,
    },
}

PIECE_VALUES = {
    chess.PAWN: 100,
    chess.KNIGHT: 320,
    chess.BISHOP: 330,
    chess.ROOK: 500,
    chess.QUEEN: 900,
    chess.KING: 20000,
}