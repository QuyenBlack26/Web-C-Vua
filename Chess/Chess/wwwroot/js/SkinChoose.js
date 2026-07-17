/* ========================= */
/* TRANG CHỌN SKIN CỜ */
/* ========================= */

let allBoardSkins = [];
let allPieceSkins = [];
let isSkinChooseReady = false;
let previewTimer = null;
const pieceDetailCache = {};

const previewPieceOrder = ["bR", "bN", "bB", "bQ", "wK", "wP", "wR", "wN"];

const previewUnicodePieces = {
    bR: "♜",
    bN: "♞",
    bB: "♝",
    bQ: "♛",
    wK: "♔",
    wP: "♙",
    wR: "♖",
    wN: "♘"
};

function getSkinElements() {
    return {
        boardSelect: document.getElementById("skinBanCoSelect"),
        pieceSelect: document.getElementById("skinQuanCoSelect"),
        previewBoard: document.getElementById("skinPreviewBoard"),
        saveButton: document.getElementById("btnSaveSkin"),
        message: document.getElementById("skinSaveMessage")
    };
}

async function fetchAllSkins() {
    try {
        const response = await fetch("/Skin/GetSkins");
        const data = await response.json();

        if (!data.success) {
            console.log("Lỗi lấy danh sách skin:", data.message);
            return false;
        }

        allBoardSkins = data.boardSkins || [];
        allPieceSkins = data.pieceSkins || [];

        return true;
    } catch (error) {
        console.log("Lỗi fetch skin:", error);
        return false;
    }
}

async function fetchMySkin() {
    try {
        const response = await fetch("/Skin/GetMySkin");
        const data = await response.json();

        if (!data.success) {
            return null;
        }

        return data;
    } catch {
        return null;
    }
}

function fillSkinSelects() {
    const { boardSelect, pieceSelect } = getSkinElements();

    if (!boardSelect || !pieceSelect) {
        return;
    }

    boardSelect.innerHTML = "";
    pieceSelect.innerHTML = "";

    allBoardSkins.forEach(function (skin) {
        const option = document.createElement("option");

        option.value = skin.skinBanCoId;
        option.textContent = skin.tenSkin;

        option.dataset.maSkin = skin.maSkin || "";
        option.dataset.mauOTrang = skin.mauOTrang || "";
        option.dataset.mauODen = skin.mauODen || "";
        option.dataset.anhNenBanCo = skin.anhNenBanCo || "";
        option.dataset.anhOSang = skin.anhOSang || "";
        option.dataset.anhODen = skin.anhODen || "";

        boardSelect.appendChild(option);
    });

    allPieceSkins.forEach(function (skin) {
        const option = document.createElement("option");

        option.value = skin.skinQuanCoId;
        option.textContent = skin.tenSkin;

        option.dataset.maSkin = skin.maSkin || "";
        option.dataset.kieuHienThi = skin.kieuHienThi || "";
        option.dataset.duongDanThuMuc = skin.duongDanThuMuc || "";
        option.dataset.cssClass = skin.cssClass || "";

        pieceSelect.appendChild(option);
    });
}

async function setCurrentSkinSelected() {
    const { boardSelect, pieceSelect } = getSkinElements();

    if (!boardSelect || !pieceSelect) {
        return;
    }

    const mySkin = await fetchMySkin();

    if (!mySkin) {
        return;
    }

    if (mySkin.boardSkin && mySkin.boardSkin.skinBanCoId) {
        boardSelect.value = mySkin.boardSkin.skinBanCoId;
    }

    if (mySkin.pieceSkin && mySkin.pieceSkin.skinQuanCoId) {
        pieceSelect.value = mySkin.pieceSkin.skinQuanCoId;
    }
}

function getSelectedBoardSkin() {
    const { boardSelect } = getSkinElements();

    if (!boardSelect) {
        return null;
    }

    return allBoardSkins.find(function (skin) {
        return String(skin.skinBanCoId) === String(boardSelect.value);
    }) || null;
}

function getSelectedPieceSkin() {
    const { pieceSelect } = getSkinElements();

    if (!pieceSelect) {
        return null;
    }

    return allPieceSkins.find(function (skin) {
        return String(skin.skinQuanCoId) === String(pieceSelect.value);
    }) || null;
}

function applyBoardPreview(boardSkin) {
    const { previewBoard } = getSkinElements();

    if (!previewBoard || !boardSkin) {
        return;
    }

    const lightColor = boardSkin.mauOTrang || "#f0d9b5";
    const darkColor = boardSkin.mauODen || "#b58863";

    previewBoard.style.setProperty("--skin-light-square", lightColor);
    previewBoard.style.setProperty("--skin-dark-square", darkColor);

    if (boardSkin.anhOSang && boardSkin.anhOSang.trim() !== "") {
        previewBoard.style.setProperty("--skin-light-image", `url("${boardSkin.anhOSang}")`);
    } else {
        previewBoard.style.setProperty("--skin-light-image", "none");
    }

    if (boardSkin.anhODen && boardSkin.anhODen.trim() !== "") {
        previewBoard.style.setProperty("--skin-dark-image", `url("${boardSkin.anhODen}")`);
    } else {
        previewBoard.style.setProperty("--skin-dark-image", "none");
    }

    previewBoard.classList.remove("skin-has-board-image");
    previewBoard.style.setProperty("--skin-board-image", "none");
}

function renderPreviewUnicodePieces() {
    const { previewBoard } = getSkinElements();

    if (!previewBoard) {
        return;
    }

    const squares = previewBoard.querySelectorAll(".preview-square");

    squares.forEach(function (square, index) {
        const code = previewPieceOrder[index];
        square.innerHTML = `<span>${previewUnicodePieces[code] || ""}</span>`;
    });
}

async function getPieceDetailData(skinQuanCoId) {
    if (pieceDetailCache[skinQuanCoId]) {
        return pieceDetailCache[skinQuanCoId];
    }

    const response = await fetch(`/Skin/GetPieceSkinDetail?skinQuanCoId=${skinQuanCoId}`);
    const data = await response.json();

    pieceDetailCache[skinQuanCoId] = data;

    return data;
}

async function applyPiecePreview(pieceSkin) {
    const { previewBoard } = getSkinElements();

    if (!previewBoard || !pieceSkin) {
        return;
    }

    const squares = previewBoard.querySelectorAll(".preview-square");

    if (pieceSkin.kieuHienThi !== "IMAGE") {
        renderPreviewUnicodePieces();
        return;
    }

    try {
        const data = await getPieceDetailData(pieceSkin.skinQuanCoId);

        if (!data.success || !data.pieces) {
            renderPreviewUnicodePieces();
            return;
        }

        squares.forEach(function (square, index) {
            const code = previewPieceOrder[index];
            const item = data.pieces[code];

            if (item && item.fullImagePath) {
                square.innerHTML = `<img class="skin-piece-img" src="${item.fullImagePath}" alt="${code}">`;
            } else {
                square.innerHTML = `<span>${previewUnicodePieces[code] || ""}</span>`;
            }
        });
    } catch {
        renderPreviewUnicodePieces();
    }
}

function updateSkinPreviewImmediately() {
    if (!isSkinChooseReady) {
        return;
    }

    clearTimeout(previewTimer);

    previewTimer = setTimeout(async function () {
        const boardSkin = getSelectedBoardSkin();
        const pieceSkin = getSelectedPieceSkin();

        applyBoardPreview(boardSkin);
        await applyPiecePreview(pieceSkin);
    }, 60);
}

async function saveSelectedSkin() {
    const { boardSelect, pieceSelect, message } = getSkinElements();

    if (!boardSelect || !pieceSelect) {
        return;
    }

    const formData = new FormData();

    formData.append("loaiCoId", "0");
    formData.append("skinBanCoId", boardSelect.value);
    formData.append("skinQuanCoId", pieceSelect.value);

    const response = await fetch("/Skin/SaveSkin", {
        method: "POST",
        body: formData
    });

    const data = await response.json();

    if (message) {
        message.textContent = data.message || "";
        message.style.color = data.success ? "#15803d" : "#b91c1c";
    }

    if (data.success) {
        updateSkinPreviewImmediately();
    }
}

function bindSkinChooseEvents() {
    const { boardSelect, pieceSelect, saveButton } = getSkinElements();

    if (boardSelect) {
        boardSelect.addEventListener("change", updateSkinPreviewImmediately);
    }

    if (pieceSelect) {
        pieceSelect.addEventListener("change", updateSkinPreviewImmediately);
    }

    if (saveButton) {
        saveButton.addEventListener("click", saveSelectedSkin);
    }
}

async function initSkinChoosePage() {
    const ok = await fetchAllSkins();

    if (!ok) {
        return;
    }

    fillSkinSelects();
    await setCurrentSkinSelected();

    isSkinChooseReady = true;

    bindSkinChooseEvents();
    updateSkinPreviewImmediately();
}

document.addEventListener("DOMContentLoaded", initSkinChoosePage);