from flask import Blueprint, request, jsonify
import traceback

from analysis.analyzer import analyze_game_moves


analysis_bp = Blueprint("analysis_bp", __name__)


@analysis_bp.route("/api/analyze-game", methods=["POST", "OPTIONS"])
def api_analyze_game():
    if request.method == "OPTIONS":
        return jsonify({"status": "ok"}), 200

    try:
        data = request.get_json(silent=True)

        if not data:
            return jsonify({
                "success": False,
                "message": "Thiếu dữ liệu gửi lên."
            }), 400

        moves = data.get("moves", [])

        if not isinstance(moves, list):
            return jsonify({
                "success": False,
                "message": "moves phải là một danh sách."
            }), 400

        result = analyze_game_moves(moves)

        return jsonify(result), 200

    except Exception as error:
        traceback.print_exc()

        return jsonify({
            "success": False,
            "message": str(error),
            "type": type(error).__name__
        }), 500