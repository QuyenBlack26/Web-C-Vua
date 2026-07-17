/* LẤY THÔNG TIN PHÒNG KHI TRANG LOAD XONG */
document.addEventListener("DOMContentLoaded", function () {
    const roomCode = sessionStorage.getItem("pvp_room_code") || "ROOM-DEMO";
    const time = sessionStorage.getItem("pvp_time") || "3";
    const color = sessionStorage.getItem("pvp_color") || "white";
    const opponent = sessionStorage.getItem("pvp_opponent") || "Đối thủ";

    document.getElementById("roomCode").textContent = roomCode;
    document.getElementById("roomTime").textContent = time + " phút";
    document.getElementById("myColor").textContent = color === "white" ? "Trắng" : "Đen";
    document.getElementById("opponentName").textContent = opponent;
});

async function saveGameAndUpdateRank(result, fen, moves, modeName, modeType, botName) {
    try {
        const response = await fetch("/Play/SaveGameHistory", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                modeName: modeName,
                modeType: modeType,
                botName: botName,
                result: result,
                fen: fen,
                moves: moves
            })
        });

        const data = await response.json();

        if (data.success) {
            console.log("Đã lưu ván và cộng điểm:", data);

            let title = "Trận đấu kết thúc";
            let message = "Kết quả đã được lưu.";
            let score = "";

            if (result === "WHITE_WIN") {
                title = "Chúc mừng!";
                message = "Quân trắng đã chiến thắng!";
                score = "+15 điểm";
            } else if (result === "BLACK_WIN") {
                title = "Chiến thắng!";
                message = "Quân đen đã chiến thắng!";
                score = "+15 điểm";
            } else {
                title = "Ván cờ hòa!";
                message = "Hai bên bất phân thắng bại.";
                score = "+5 điểm";
            }

            showGameResultModal({
                icon: result === "DRAW" ? "🤝" : "🏆",
                title: title,
                message: message,
                score: score
            });
        }
    } catch (error) {
        console.error("Lỗi gọi SaveGameHistory:", error);
        alert("Không thể lưu ván cờ và cộng điểm.");
    }
}