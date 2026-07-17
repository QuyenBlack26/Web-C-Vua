/* BIẾN LƯU LỰA CHỌN GHÉP TRẬN */
let selectedColor = "white";
let selectedTime = "3";
let searchTimer = null;

/* CHUYỂN MÃ MÀU QUÂN THÀNH CHỮ HIỂN THỊ */
const colorTextMap = {
    white: "Trắng",
    black: "Đen",
    random: "Ngẫu nhiên"
};

/* XỬ LÝ CHỌN MÀU QUÂN */
document.querySelectorAll("#colorChoices .choice").forEach(button => {
    button.addEventListener("click", function () {
        document.querySelectorAll("#colorChoices .choice").forEach(btn => btn.classList.remove("active"));
        this.classList.add("active");
        selectedColor = this.dataset.color;
    });
});

/* XỬ LÝ CHỌN THỜI GIAN */
document.querySelectorAll("#timeChoices .choice").forEach(button => {
    button.addEventListener("click", function () {
        document.querySelectorAll("#timeChoices .choice").forEach(btn => btn.classList.remove("active"));
        this.classList.add("active");
        selectedTime = this.dataset.time;
    });
});

/* BẮT ĐẦU GHÉP TRẬN */
function startMatchmaking() {
    document.getElementById("setupCard").classList.add("hidden");
    document.getElementById("foundCard").classList.add("hidden");
    document.getElementById("searchingCard").classList.remove("hidden");

    document.getElementById("selectedColorText").textContent = colorTextMap[selectedColor];
    document.getElementById("selectedTimeText").textContent = selectedTime + " phút";
    document.getElementById("matchStatusText").textContent = "Đang ghép trận";

    const searchText = document.getElementById("searchText");

    let step = 0;
    const messages = [
        "Đang kết nối tới hàng chờ PvP online...",
        "Đang tìm người chơi có cùng thời gian...",
        "Đang tạo phòng đấu...",
        "Đã tìm thấy đối thủ!"
    ];

    searchText.textContent = messages[0];

    searchTimer = setInterval(() => {
        step++;

        if (step < messages.length) {
            searchText.textContent = messages[step];
        }

        if (step === messages.length - 1) {
            clearInterval(searchTimer);

            setTimeout(() => {
                showMatchFound();
            }, 700);
        }
    }, 900);
}

/* HIỂN THỊ KHI TÌM THẤY ĐỐI THỦ */
function showMatchFound() {
    const roomCode = generateRoomCode();
    const opponentNames = ["KnightPlayer", "ChessMaster", "QueenSide", "RapidKing", "BotLikeHuman"];
    const opponentName = opponentNames[Math.floor(Math.random() * opponentNames.length)];

    let myColor = selectedColor;

    if (myColor === "random") {
        myColor = Math.random() > 0.5 ? "white" : "black";
    }

    const opponentColor = myColor === "white" ? "black" : "white";

    document.getElementById("searchingCard").classList.add("hidden");
    document.getElementById("foundCard").classList.remove("hidden");

    document.getElementById("roomCode").textContent = roomCode;
    document.getElementById("roomTime").textContent = selectedTime + " phút";
    document.getElementById("opponentName").textContent = opponentName;
    document.getElementById("myColorText").textContent = myColor === "white" ? "Trắng" : "Đen";
    document.getElementById("opponentColorText").textContent = opponentColor === "white" ? "Trắng" : "Đen";

    sessionStorage.setItem("pvp_room_code", roomCode);
    sessionStorage.setItem("pvp_time", selectedTime);
    sessionStorage.setItem("pvp_color", myColor);
    sessionStorage.setItem("pvp_opponent", opponentName);
}

/* HỦY GHÉP TRẬN */
function cancelMatchmaking() {
    if (searchTimer) {
        clearInterval(searchTimer);
    }

    document.getElementById("searchingCard").classList.add("hidden");
    document.getElementById("foundCard").classList.add("hidden");
    document.getElementById("setupCard").classList.remove("hidden");
}

/* GHÉP LẠI TRẬN MỚI */
function resetMatchmaking() {
    document.getElementById("foundCard").classList.add("hidden");
    document.getElementById("searchingCard").classList.add("hidden");
    document.getElementById("setupCard").classList.remove("hidden");
}

/* VÀO PHÒNG CHƠI */
function enterRoom() {
    window.location.href = "/Play/Room";
}

/* TẠO MÃ PHÒNG NGẪU NHIÊN */
function generateRoomCode() {
    const chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
    let code = "ROOM-";

    for (let i = 0; i < 5; i++) {
        code += chars[Math.floor(Math.random() * chars.length)];
    }

    return code;
}

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
        } else {
            console.error("Lỗi lưu ván:", data.message);
            alert(data.message);
        }
    } catch (error) {
        console.error("Lỗi gọi SaveGameHistory:", error);
        alert("Không thể lưu ván cờ và cộng điểm.");
    }
}