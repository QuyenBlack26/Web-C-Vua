/* ========================= */
/* MODAL KẾT QUẢ TRẬN ĐẤU */
/* ========================= */

function showGameResultModal(options) {
    const overlay = document.getElementById("gameResultOverlay");
    const icon = document.getElementById("gameResultIcon");
    const title = document.getElementById("gameResultTitle");
    const message = document.getElementById("gameResultMessage");
    const score = document.getElementById("gameResultScore");

    if (!overlay || !icon || !title || !message || !score) {
        alert(options?.message || "Trận đấu đã kết thúc.");
        return;
    }

    icon.textContent = options?.icon || "🏆";
    title.textContent = options?.title || "Trận đấu kết thúc";
    message.textContent = options?.message || "Kết quả đã được ghi nhận.";
    score.textContent = options?.score || "";

    if (!options?.score) {
        score.style.display = "none";
    } else {
        score.style.display = "inline-flex";
    }

    overlay.classList.add("show");
}

function hideGameResultModal() {
    const overlay = document.getElementById("gameResultOverlay");

    if (overlay) {
        overlay.classList.remove("show");
    }
}

document.addEventListener("DOMContentLoaded", function () {
    const closeBtn = document.getElementById("gameResultCloseBtn");
    const overlay = document.getElementById("gameResultOverlay");

    if (closeBtn) {
        closeBtn.addEventListener("click", hideGameResultModal);
    }

    if (overlay) {
        overlay.addEventListener("click", function (event) {
            if (event.target === overlay) {
                hideGameResultModal();
            }
        });
    }
});