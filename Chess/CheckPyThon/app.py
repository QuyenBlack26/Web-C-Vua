from flask import Flask
from flask_cors import CORS

from routes import register_routes
from analysis.analysis_routes import analysis_bp


app = Flask(__name__)

CORS(
    app,
    resources={r"/api/*": {"origins": "https://localhost:7140"}},
    methods=["GET", "POST", "OPTIONS"],
    allow_headers=["Content-Type", "Authorization"]
)

register_routes(app)

app.register_blueprint(analysis_bp)


@app.route("/")
def home():
    return {
        "status": "ok",
        "message": "Chess AI đang chạy",
        "api": "http://localhost:5000/api",
        "analysis_api": "http://localhost:5000/api/analyze-game"
    }


if __name__ == "__main__":
    print("Chess AI API: http://localhost:5000")
    print("Analysis API: http://localhost:5000/api/analyze-game")
    app.run(host="0.0.0.0", port=5000, debug=True)