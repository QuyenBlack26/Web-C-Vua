from flask import Flask, request, jsonify
from flask_cors import CORS
import chess
import random
import math

app = Flask(__name__)
CORS(app)

PIECE_VALUES = {
    chess.PAWN: 100,
    chess.KNIGHT: 320,
    chess.BISHOP: 330,
    chess.ROOK: 500,
    chess.QUEEN: 900,
    chess.KING: 20000,
}

def evaluate_board(board):
    if board.is_checkmate():
        return -99999 if board.turn == chess.WHITE else 99999
    if board.is_stalemate() or board.is_insufficient_material():
        return 0

    score = 0
    for square in chess.SQUARES:
        piece = board.piece_at(square)
        if piece:
            value = PIECE_VALUES[piece.piece_type]
            score += value if piece.color == chess.WHITE else -value
    return score

def minimax(board, depth, alpha, beta, maximizing):
    if depth == 0 or board.is_game_over():
        return evaluate_board(board)

    if maximizing:
        best = -math.inf
        for move in board.legal_moves:
            board.push(move)
            best = max(best, minimax(board, depth - 1, alpha, beta, False))
            board.pop()
            alpha = max(alpha, best)
            if beta <= alpha:
                break
        return best
    else:
        best = math.inf
        for move in board.legal_moves:
            board.push(move)
            best = min(best, minimax(board, depth - 1, alpha, beta, True))
            board.pop()
            beta = min(beta, best)
            if beta <= alpha:
                break
        return best

def get_best_move(board, level="medium"):
    moves = list(board.legal_moves)
    if not moves:
        return None

    depth_map = {
        "easy": 1,
        "medium": 2,
        "hard": 3
    }

    if level == "easy" and random.random() < 0.5:
        return random.choice(moves)

    depth = depth_map.get(level, 2)

    is_white = board.turn == chess.WHITE
    best_move = moves[0]
    best_score = -math.inf if is_white else math.inf

    for move in moves:
        board.push(move)
        score = minimax(board, depth - 1, -math.inf, math.inf, not is_white)
        board.pop()

        if is_white and score > best_score:
            best_score = score
            best_move = move

        if not is_white and score < best_score:
            best_score = score
            best_move = move

    return best_move

@app.route("/api/move", methods=["POST"])
def ai_move():
    data = request.get_json()

    fen = data.get("fen")
    level = data.get("level", "medium")

    try:
        board = chess.Board(fen)
    except:
        return jsonify({"error": "FEN không hợp lệ"}), 400

    move = get_best_move(board, level)

    if move is None:
        return jsonify({
            "move": None,
            "is_game_over": True
        })

    move_san = board.san(move)
    board.push(move)

    return jsonify({
        "move": move.uci(),
        "move_san": move_san,
        "fen_after": board.fen(),
        "score": evaluate_board(board),
        "is_check": board.is_check(),
        "is_checkmate": board.is_checkmate(),
        "is_game_over": board.is_game_over()
    })

@app.route("/api/validate", methods=["POST"])
def validate_move():
    data = request.get_json()

    fen = data.get("fen")
    move_text = data.get("move")

    try:
        board = chess.Board(fen)
        move = chess.Move.from_uci(move_text)
    except:
        return jsonify({"valid": False})

    if move not in board.legal_moves:
        return jsonify({"valid": False})

    board.push(move)

    return jsonify({
        "valid": True,
        "fen_after": board.fen(),
        "is_check": board.is_check(),
        "is_checkmate": board.is_checkmate(),
        "is_game_over": board.is_game_over()
    })

@app.route("/api/legal-moves", methods=["POST"])
def legal_moves():
    data = request.get_json()

    fen = data.get("fen")
    square = data.get("square")

    try:
        board = chess.Board(fen)
        sq = chess.parse_square(square)
    except:
        return jsonify({"legal_moves": []})

    moves = [
        move.uci()
        for move in board.legal_moves
        if move.from_square == sq
    ]

    return jsonify({"legal_moves": moves})

@app.route("/")
def home():
    return "Chess AI API đang chạy!"

if __name__ == "__main__":
    app.run(debug=True, port=5000)