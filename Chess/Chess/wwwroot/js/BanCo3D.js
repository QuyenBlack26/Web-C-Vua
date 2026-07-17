import * as THREE from "three";
import { OrbitControls } from "three/addons/controls/OrbitControls.js";
import { GLTFLoader } from "three/addons/loaders/GLTFLoader.js";
import ChessAI from "./chessai.js";

/* LẤY PHẦN TỬ GIAO DIỆN */
const ai3dLevelSelect = document.getElementById("ai3dLevelSelect");
const ai3dColorSelect = document.getElementById("ai3dColorSelect");
const ai3dStartBtn = document.getElementById("ai3dStartBtn");
const ai3dSceneContainer = document.getElementById("ai3dSceneContainer");
const ai3dStatusText = document.getElementById("ai3dStatusText");
const ai3dSideText = document.getElementById("ai3dSideText");
const ai3dNewGameBtn = document.getElementById("ai3dNewGameBtn");
const ai3dResignBtn = document.getElementById("ai3dResignBtn");
const ai3dMoveHistory = document.getElementById("ai3dMoveHistory");
const ai3dPromotionOverlay = document.getElementById("ai3dPromotionOverlay");
const ai3dPromotionChoices = document.querySelectorAll(".ai3d-promotion-choice");

/* TRẠNG THÁI GAME */
let ai3dLevel = "medium";
let ai3dPlayerColor = "white";
let ai3dAIColor = "black";

let fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKQkq - 0 1";
fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";

let fenHistory = [fen];
let lastAiMove = null;

let ai = null;
let selectedPiece = null;
let isAnimating = false;
let isBusy = false;
let gameOver = false;
let historySaved = false;
let moveHistoryForSave = [];
let moveHistoryDisplay = [];
let pendingPromotionMove = "";

let scene = null;
let camera = null;
let renderer = null;
let controls = null;
let animationId = null;

const boardGroup = new THREE.Group();
const pieceGroup = new THREE.Group();

const raycaster = new THREE.Raycaster();
const mouse = new THREE.Vector2();

/* PHÂN BIỆT CLICK CHỌN QUÂN VÀ KÉO XOAY CAMERA */
let pointerStartX = 0;
let pointerStartY = 0;
let pointerStartTime = 0;
let isPointerDragging = false;

const CLICK_MOVE_LIMIT = 16;
const CLICK_TIME_LIMIT = 800;

const boardSquares = new Map();
const piecesBySquare = new Map();

/* LƯU DANH SÁCH Ô ĐƯỢC GỢI Ý */
let legalMoveSquares = [];

/* LOAD MODEL QUÂN CỜ 3D TỪ BLENDER */
const gltfLoader = new GLTFLoader();
const pieceModelCache = new Map();

/* ĐƯỜNG DẪN THEO TÊN FILE CỦA BẠN TRONG wwwroot/IMG/QuanCo */
const pieceModelPaths = {
    white: {
        P: "/IMG/QuanCo/Tot.glb",
        R: "/IMG/QuanCo/Xe.glb",
        N: "/IMG/QuanCo/Ngua.glb",
        B: "/IMG/QuanCo/Tuong.glb",
        Q: "/IMG/QuanCo/Hau.glb",
        K: "/IMG/QuanCo/Vua.glb"
    },

    black: {
        P: "/IMG/QuanCo/Totd.glb",
        R: "/IMG/QuanCo/Xed.glb",
        N: "/IMG/QuanCo/Nguad.glb",
        B: "/IMG/QuanCo/Tuongd.glb",
        Q: "/IMG/QuanCo/Haud.glb",
        K: "/IMG/QuanCo/Vuad.glb"
    }
};

/* VẬT LIỆU */
const materials = {
    boardFrame: new THREE.MeshStandardMaterial({
        color: 0x1f2937,
        roughness: 0.55,
        metalness: 0.08
    }),

    whitePiece: new THREE.MeshStandardMaterial({
        color: 0xf8fafc,
        roughness: 0.35,
        metalness: 0.12
    }),

    blackPiece: new THREE.MeshStandardMaterial({
        color: 0x111827,
        roughness: 0.42,
        metalness: 0.18
    }),

    gold: new THREE.MeshStandardMaterial({
        color: 0xfacc15,
        roughness: 0.38,
        metalness: 0.25
    })
};

/* MAP QUÂN TỪ FEN SANG TYPE/COLOR */
const fenPieceMap = {
    P: { type: "P", color: "white" },
    R: { type: "R", color: "white" },
    N: { type: "N", color: "white" },
    B: { type: "B", color: "white" },
    Q: { type: "Q", color: "white" },
    K: { type: "K", color: "white" },

    p: { type: "P", color: "black" },
    r: { type: "R", color: "black" },
    n: { type: "N", color: "black" },
    b: { type: "B", color: "black" },
    q: { type: "Q", color: "black" },
    k: { type: "K", color: "black" }
};

/* CẬP NHẬT TRẠNG THÁI */
function setAI3DStatus(message) {
    if (ai3dStatusText) {
        ai3dStatusText.textContent = message;
    }
}

/* LẤY LƯỢT HIỆN TẠI TỪ FEN */
function getFenTurn() {
    return fen.split(" ")[1] === "w" ? "white" : "black";
}

/* CHUYỂN Ô CỜ SANG TỌA ĐỘ 3D */
function squareToPosition(square) {
    const files = "abcdefgh";
    const file = files.indexOf(square[0]);
    const rank = Number(square[1]);

    return new THREE.Vector3(file - 3.5, 0, rank - 4.5);
}

/* CHUYỂN TỌA ĐỘ BÀN SANG TÊN Ô */
function getSquareName(fileIndex, rankIndex) {
    const files = "abcdefgh";
    return files[fileIndex] + (rankIndex + 1);
}

/* CHUYỂN FEN THÀNH DANH SÁCH QUÂN */
function fenToPieces(fenText) {
    const boardPart = fenText.split(" ")[0];
    const rows = boardPart.split("/");
    const result = [];

    for (let row = 0; row < 8; row++) {
        const fenRow = rows[row];
        let file = 0;
        const rank = 8 - row;

        for (const char of fenRow) {
            if (!Number.isNaN(Number(char))) {
                file += Number(char);
                continue;
            }

            const info = fenPieceMap[char];

            if (!info) {
                continue;
            }

            const square = "abcdefgh"[file] + rank;

            result.push({
                square: square,
                type: info.type,
                color: info.color
            });

            file++;
        }
    }

    return result;
}

/* XÓA SCENE CŨ */
function disposeOldScene() {
    if (animationId) {
        cancelAnimationFrame(animationId);
        animationId = null;
    }

    if (renderer) {
        renderer.dispose();

        if (renderer.domElement && renderer.domElement.parentNode) {
            renderer.domElement.parentNode.removeChild(renderer.domElement);
        }
    }

    scene = null;
    camera = null;
    renderer = null;
    controls = null;
    selectedPiece = null;

    boardGroup.clear();
    pieceGroup.clear();
    boardSquares.clear();
    piecesBySquare.clear();
    legalMoveSquares = [];
}

/* KHỞI TẠO SCENE */
async function initScene() {
    if (!ai3dSceneContainer) {
        return;
    }

    disposeOldScene();

    ai3dSceneContainer.innerHTML = "";

    scene = new THREE.Scene();
    scene.background = new THREE.Color(0xdbeafe);

    const width = ai3dSceneContainer.clientWidth;
    const height = ai3dSceneContainer.clientHeight;

    camera = new THREE.PerspectiveCamera(45, width / height, 0.1, 100);

    if (ai3dPlayerColor === "white") {
        camera.position.set(0, 8.5, -9.5);
    } else {
        camera.position.set(0, 8.5, 9.5);
    }

    camera.lookAt(0, 0, 0);

    renderer = new THREE.WebGLRenderer({
        antialias: true
    });

    renderer.setSize(width, height);
    renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
    renderer.shadowMap.enabled = true;
    renderer.shadowMap.type = THREE.PCFSoftShadowMap;

    ai3dSceneContainer.appendChild(renderer.domElement);

    controls = new OrbitControls(camera, renderer.domElement);
    controls.target.set(0, 0, 0);
    controls.enableDamping = true;
    controls.dampingFactor = 0.08;
    controls.minDistance = 6;
    controls.maxDistance = 18;
    controls.maxPolarAngle = Math.PI * 0.48;

    /* CHO PHÉP XOAY BÀN CỜ NHƯNG KHÔNG CHO PAN LỆCH BÀN */
    controls.enableRotate = true;
    controls.enablePan = false;
    controls.enableZoom = true;

    controls.update();

    addLights();
    createBoard();
    await createPiecesFromFen(fen);

    scene.add(boardGroup);
    scene.add(pieceGroup);

    renderer.domElement.addEventListener("pointerdown", handlePointerDown);
    renderer.domElement.addEventListener("pointermove", handlePointerMove);
    renderer.domElement.addEventListener("pointerup", handlePointerUp);
    renderer.domElement.addEventListener("pointercancel", handlePointerCancel);

    animate();
}

/* THÊM ĐÈN */
function addLights() {
    const ambientLight = new THREE.AmbientLight(0xffffff, 0.68);
    scene.add(ambientLight);

    const keyLight = new THREE.DirectionalLight(0xffffff, 1.4);
    keyLight.position.set(3, 8, 4);
    keyLight.castShadow = true;
    keyLight.shadow.mapSize.width = 2048;
    keyLight.shadow.mapSize.height = 2048;
    scene.add(keyLight);

    const fillLight = new THREE.DirectionalLight(0x93c5fd, 0.65);
    fillLight.position.set(-5, 5, -4);
    scene.add(fillLight);
}

/* TẠO MATERIAL Ô CỜ RIÊNG */
function createSquareMaterial(color) {
    return new THREE.MeshStandardMaterial({
        color: color,
        roughness: 0.72,
        emissive: 0x000000,
        emissiveIntensity: 0
    });
}

/* RESET HIGHLIGHT Ô */
function resetSquareHighlights() {
    boardSquares.forEach(function (square) {
        square.material.emissive.setHex(0x000000);
        square.material.emissiveIntensity = 0;
    });

    legalMoveSquares = [];
}

/* HIGHLIGHT Ô ĐANG CHỌN */
function highlightSquare(squareName) {
    resetSquareHighlights();

    const square = boardSquares.get(squareName);

    if (!square) {
        return;
    }

    square.material.emissive.setHex(0x22c55e);
    square.material.emissiveIntensity = 0.45;
}

/* HIỆN GỢI Ý NƯỚC ĐI HỢP LỆ */
function highlightLegalMoves(fromSquare, legalMoves) {
    legalMoveSquares = [];

    const selectedSquare = boardSquares.get(fromSquare);

    if (selectedSquare) {
        selectedSquare.material.emissive.setHex(0x22c55e);
        selectedSquare.material.emissiveIntensity = 0.45;
    }

    legalMoves.forEach(function (move) {
        const toSquare = move.substring(2, 4);
        const square = boardSquares.get(toSquare);

        if (!square) {
            return;
        }

        const hasPiece = piecesBySquare.has(toSquare);

        if (hasPiece) {
            square.material.emissive.setHex(0xef4444);
            square.material.emissiveIntensity = 0.55;
        } else {
            square.material.emissive.setHex(0x3b82f6);
            square.material.emissiveIntensity = 0.42;
        }

        legalMoveSquares.push(toSquare);
    });
}

/* LẤY GỢI Ý NƯỚC ĐI TỪ PYTHON API */
async function getLegalMovesForSquare(squareName) {
    try {
        const response = await fetch("http://localhost:5000/api/legal-moves", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify({
                fen: fen,
                square: squareName
            })
        });

        const data = await response.json();

        if (!response.ok) {
            console.error("Lỗi lấy legal moves:", data);
            return [];
        }

        return data.legal_moves || [];
    } catch (error) {
        console.error("Không gọi được API legal-moves:", error);
        return [];
    }
}

/* TẠO BÀN CỜ */
function createBoard() {
    boardGroup.clear();

    const frameGeometry = new THREE.BoxGeometry(9.2, 0.28, 9.2);
    const frame = new THREE.Mesh(frameGeometry, materials.boardFrame);
    frame.position.y = -0.18;
    frame.receiveShadow = true;
    boardGroup.add(frame);

    for (let file = 0; file < 8; file++) {
        for (let rankIndex = 0; rankIndex < 8; rankIndex++) {
            const squareName = getSquareName(file, rankIndex);
            const squareGeometry = new THREE.BoxGeometry(1, 0.12, 1);
            const isLight = (file + rankIndex) % 2 === 0;

            const square = new THREE.Mesh(
                squareGeometry,
                createSquareMaterial(isLight ? 0xf0d9b5 : 0xb58863)
            );

            square.position.set(file - 3.5, 0, rankIndex - 3.5);
            square.receiveShadow = true;
            square.userData.kind = "square";
            square.userData.square = squareName;

            boardSquares.set(squareName, square);
            boardGroup.add(square);
        }
    }
}

/* LOAD 1 MODEL GLB VÀ CACHE LẠI */
function loadPieceModel(type, color) {
    return new Promise(function (resolve, reject) {
        const path = pieceModelPaths[color]?.[type];

        if (!path) {
            reject("Không tìm thấy đường dẫn model cho quân: " + color + " " + type);
            return;
        }

        const cacheKey = color + "_" + type;

        if (pieceModelCache.has(cacheKey)) {
            resolve(pieceModelCache.get(cacheKey).clone(true));
            return;
        }

        gltfLoader.load(
            path,
            function (gltf) {
                const model = gltf.scene;
                pieceModelCache.set(cacheKey, model);
                resolve(model.clone(true));
            },
            undefined,
            function (error) {
                reject(error);
            }
        );
    });
}

/* TẠO QUÂN 3D TỪ MODEL BLENDER */
async function createPieceMesh(type, color) {
    const group = new THREE.Group();

    let model = null;

    try {
        model = await loadPieceModel(type, color);
    } catch (error) {
        console.error("Không load được model quân:", type, color, error);
        model = createFallbackPieceMesh(type, color);
    }

    model.traverse(function (item) {
        if (item.isMesh) {
            item.castShadow = true;
            item.receiveShadow = true;
        }
    });

    model.position.set(0, 0, 0);
    model.rotation.set(0, 0, 0);
    model.scale.set(1, 1, 1);

    let box = new THREE.Box3().setFromObject(model);
    const size = box.getSize(new THREE.Vector3());
    const center = box.getCenter(new THREE.Vector3());

    model.position.x -= center.x;
    model.position.z -= center.z;
    model.position.y -= box.min.y;

    const maxSize = Math.max(size.x, size.y, size.z);
    const targetSize = 0.78;
    const scale = targetSize / maxSize;

    model.scale.set(scale, scale, scale);

    box = new THREE.Box3().setFromObject(model);

    model.position.x -= (box.min.x + box.max.x) / 2;
    model.position.z -= (box.min.z + box.max.z) / 2;
    model.position.y -= box.min.y;

    group.add(model);

    group.userData.kind = "piece";
    group.userData.type = type;
    group.userData.color = color;

    group.traverse(function (item) {
        item.userData.rootPiece = group;
    });

    return group;
}

/* QUÂN DỰ PHÒNG KHI MODEL GLB LOAD LỖI */
function createFallbackPieceMesh(type, color) {
    const group = new THREE.Group();
    const material = color === "white" ? materials.whitePiece : materials.blackPiece;

    const base = new THREE.Mesh(
        new THREE.CylinderGeometry(0.38, 0.42, 0.16, 32),
        material
    );
    base.position.y = 0.08;
    base.castShadow = true;
    base.receiveShadow = true;
    group.add(base);

    const body = new THREE.Mesh(
        new THREE.CylinderGeometry(0.24, 0.32, 0.65, 32),
        material
    );
    body.position.y = 0.48;
    body.castShadow = true;
    body.receiveShadow = true;
    group.add(body);

    let headGeometry;

    if (type === "P") {
        headGeometry = new THREE.SphereGeometry(0.24, 32, 32);
    } else if (type === "R") {
        headGeometry = new THREE.BoxGeometry(0.46, 0.28, 0.46);
    } else if (type === "N") {
        headGeometry = new THREE.ConeGeometry(0.28, 0.48, 4);
    } else if (type === "B") {
        headGeometry = new THREE.ConeGeometry(0.28, 0.58, 32);
    } else if (type === "Q") {
        headGeometry = new THREE.SphereGeometry(0.3, 32, 32);
    } else {
        headGeometry = new THREE.BoxGeometry(0.38, 0.38, 0.38);
    }

    const head = new THREE.Mesh(headGeometry, material);
    head.position.y = 0.95;
    head.castShadow = true;
    head.receiveShadow = true;
    group.add(head);

    const crown = new THREE.Mesh(
        new THREE.CylinderGeometry(0.14, 0.18, 0.08, 24),
        materials.gold
    );

    crown.position.y = 1.25;
    crown.castShadow = true;

    if (type === "K" || type === "Q" || type === "B") {
        group.add(crown);
    }

    return group;
}

/* TẠO QUÂN TỪ FEN */
async function createPiecesFromFen(fenText) {
    pieceGroup.clear();
    piecesBySquare.clear();

    const pieces = fenToPieces(fenText);

    for (const item of pieces) {
        const piece = await createPieceMesh(item.type, item.color);
        const position = squareToPosition(item.square);

        piece.position.set(position.x, 0.05, position.z);
        piece.userData.square = item.square;

        piecesBySquare.set(item.square, piece);
        pieceGroup.add(piece);
    }
}

/* LẤY ROOT PIECE */
function getRootPieceFromObject(object) {
    let current = object;

    while (current) {
        if (current.userData && current.userData.kind === "piece") {
            return current;
        }

        if (current.userData && current.userData.rootPiece) {
            return current.userData.rootPiece;
        }

        current = current.parent;
    }

    return null;
}

/* BẮT ĐẦU NHẤN CHUỘT */
function handlePointerDown(event) {
    pointerStartX = event.clientX;
    pointerStartY = event.clientY;
    pointerStartTime = performance.now();
    isPointerDragging = false;
}

/* KIỂM TRA CÓ ĐANG KÉO CAMERA KHÔNG */
function handlePointerMove(event) {
    const dx = Math.abs(event.clientX - pointerStartX);
    const dy = Math.abs(event.clientY - pointerStartY);

    if (dx > CLICK_MOVE_LIMIT || dy > CLICK_MOVE_LIMIT) {
        isPointerDragging = true;
    }
}

/* HỦY POINTER */
function handlePointerCancel() {
    isPointerDragging = false;
}

/* KẾT THÚC NHẤN CHUỘT */
function handlePointerUp(event) {
    const pressTime = performance.now() - pointerStartTime;
    const dx = Math.abs(event.clientX - pointerStartX);
    const dy = Math.abs(event.clientY - pointerStartY);

    const isClick =
        dx <= CLICK_MOVE_LIMIT &&
        dy <= CLICK_MOVE_LIMIT &&
        pressTime <= CLICK_TIME_LIMIT;

    if (!isClick || isPointerDragging) {
        return;
    }

    handleSceneClick(event);
}

/* CLICK TRONG SCENE */
function handleSceneClick(event) {
    if (!renderer || !camera || isAnimating || isBusy || gameOver) {
        return;
    }

    const rect = renderer.domElement.getBoundingClientRect();

    mouse.x = ((event.clientX - rect.left) / rect.width) * 2 - 1;
    mouse.y = -((event.clientY - rect.top) / rect.height) * 2 + 1;

    raycaster.setFromCamera(mouse, camera);

    /* ƯU TIÊN BẮT QUÂN TRƯỚC */
    const pieceHits = raycaster.intersectObjects(pieceGroup.children, true);

    if (pieceHits.length > 0) {
        const rootPiece = getRootPieceFromObject(pieceHits[0].object);

        if (rootPiece) {
            handlePieceClick(rootPiece);
            return;
        }
    }

    /* SAU ĐÓ MỚI BẮT Ô CỜ, KHÔNG BẮT FRAME */
    const squareHits = raycaster.intersectObjects([...boardSquares.values()], true);

    if (squareHits.length > 0) {
        const squareName = squareHits[0].object.userData.square;

        if (squareName) {
            handleSquareClick(squareName);
            return;
        }
    }

    clearSelection();
}

/* CLICK QUÂN */
async function handlePieceClick(piece) {
    const turn = getFenTurn();

    if (turn !== ai3dPlayerColor) {
        setAI3DStatus("Chưa tới lượt của bạn. AI đang đi hoặc ván chưa sẵn sàng.");
        return;
    }

    const color = piece.userData.color;
    const square = piece.userData.square;

    if (color === ai3dPlayerColor) {
        selectedPiece = piece;
        resetSquareHighlights();

        const legalMoves = await getLegalMovesForSquare(square);

        if (legalMoves.length === 0) {
            highlightSquare(square);
            setAI3DStatus("Quân ở ô " + square + " hiện không có nước đi hợp lệ.");
            return;
        }

        highlightLegalMoves(square, legalMoves);
        setAI3DStatus("Đã chọn quân ở ô " + square + ". Ô xanh là đi được, ô đỏ là ăn được.");
        return;
    }

    if (selectedPiece) {
        if (legalMoveSquares.length > 0 && !legalMoveSquares.includes(square)) {
            setAI3DStatus("Quân này không nằm trong nước ăn hợp lệ.");
            return;
        }

        tryPlayerMove(selectedPiece.userData.square + square);
        return;
    }

    setAI3DStatus("Đó là quân của AI. Bạn chỉ được chọn quân của mình.");
}

/* CLICK Ô */
function handleSquareClick(squareName) {
    const turn = getFenTurn();

    if (turn !== ai3dPlayerColor) {
        setAI3DStatus("Chưa tới lượt của bạn.");
        return;
    }

    if (!selectedPiece) {
        setAI3DStatus("Hãy chọn quân trước.");
        return;
    }

    if (legalMoveSquares.length > 0 && !legalMoveSquares.includes(squareName)) {
        setAI3DStatus("Ô này không phải nước đi hợp lệ. Hãy chọn ô đang được gợi ý.");
        return;
    }

    const move = selectedPiece.userData.square + squareName;

    if (isPromotionMove(move)) {
        showAI3DPromotionModal(move);
        return;
    }

    tryPlayerMove(move);
}

/* XÓA CHỌN */
function clearSelection() {
    selectedPiece = null;
    resetSquareHighlights();
}

/* KIỂM TRA NƯỚC ĐI CÓ CẦN PHONG CẤP KHÔNG */
function isPromotionMove(move) {
    if (!move || move.length < 4) {
        return false;
    }

    const from = move.substring(0, 2);
    const to = move.substring(2, 4);
    const piece = piecesBySquare.get(from);

    if (!piece) {
        return false;
    }

    const type = piece.userData.type;
    const color = piece.userData.color;

    if (type !== "P") {
        return false;
    }

    if (color === "white" && from[1] === "7" && to[1] === "8") {
        return true;
    }

    if (color === "black" && from[1] === "2" && to[1] === "1") {
        return true;
    }

    return false;
}

/* HIỆN MODAL PHONG CẤP */
function showAI3DPromotionModal(baseMove) {
    pendingPromotionMove = baseMove;

    if (ai3dPromotionOverlay) {
        ai3dPromotionOverlay.classList.add("show");
        return;
    }

    tryPlayerMove(baseMove + "q");
}

/* ẨN MODAL PHONG CẤP */
function hideAI3DPromotionModal() {
    if (ai3dPromotionOverlay) {
        ai3dPromotionOverlay.classList.remove("show");
    }
}

/* GẮN SỰ KIỆN CHỌN QUÂN PHONG CẤP */
ai3dPromotionChoices.forEach(function (button) {
    button.addEventListener("click", function () {
        if (!pendingPromotionMove) {
            return;
        }

        const promotionPiece = button.dataset.piece || "q";
        const finalMove = pendingPromotionMove + promotionPiece;

        pendingPromotionMove = "";
        hideAI3DPromotionModal();

        tryPlayerMove(finalMove);
    });
});

/* THỬ ĐI NƯỚC NGƯỜI CHƠI */
async function tryPlayerMove(move) {
    if (!ai || isBusy || gameOver) {
        return;
    }

    isBusy = true;
    resetSquareHighlights();

    const fenBefore = fen;

    setAI3DStatus("Đang kiểm tra nước đi " + move + "...");

    try {
        const result = await ai.validateMove(fenBefore, move);

        if (!result || !result.valid) {
            setAI3DStatus(result?.error || "Nước đi không hợp lệ.");
            isBusy = false;
            selectedPiece = null;
            return;
        }

        const from = move.substring(0, 2);
        const to = move.substring(2, 4);

        await applyMove3D(from, to);
        addAI3DHistory("Bạn", move, "player");

        fen = result.fen_after;
        fenHistory.push(fen);
        selectedPiece = null;

        if (move.length > 4) {
            await createPiecesFromFen(fen);
        }

        if (result.is_checkmate) {
            gameOver = true;
            setAI3DStatus("Chiếu hết! Bạn thắng.");
            saveAI3DHistory("WHITE_WIN", "Bạn đã chiếu hết AI 3D.");
            return;
        }

        if (result.is_game_over || result.is_stalemate) {
            gameOver = true;
            setAI3DStatus("Ván cờ kết thúc.");
            saveAI3DHistory("DRAW", "Ván cờ AI 3D kết thúc với kết quả hòa.");
            return;
        }

        setAI3DStatus("AI đang suy nghĩ...");
        await aiMove();
    } catch (error) {
        console.error("Lỗi validate nước đi 3D:", error);
        setAI3DStatus("Lỗi kết nối khi kiểm tra nước đi.");
    } finally {
        if (!gameOver) {
            isBusy = false;
        }
    }
}

/* AI ĐI */
async function aiMove() {
    if (!ai || gameOver) {
        return;
    }

    const fenBeforeAI = fen;

    try {
        const result = await ai.getMove(fenBeforeAI, fenHistory, lastAiMove);

        if (!result || result.error) {
            setAI3DStatus(result?.error || "AI không trả về nước đi.");
            return;
        }

        if (!result.move || result.move.length < 4) {
            gameOver = true;
            setAI3DStatus("AI không còn nước đi hợp lệ. Ván cờ kết thúc.");
            return;
        }

        const from = result.move.substring(0, 2);
        const to = result.move.substring(2, 4);

        await applyMove3D(from, to);
        addAI3DHistory("AI", result.move, "ai");

        fen = result.fen_after;
        fenHistory.push(fen);
        lastAiMove = result.move;

        if (result.move.length > 4) {
            await createPiecesFromFen(fen);
        }

        if (result.is_checkmate) {
            gameOver = true;
            setAI3DStatus("Chiếu hết! AI thắng.");
            saveAI3DHistory("BLACK_WIN", "AI 3D đã chiếu hết bạn.");
            return;
        }

        if (result.is_game_over || result.is_stalemate) {
            gameOver = true;
            setAI3DStatus("Ván cờ kết thúc.");
            saveAI3DHistory("DRAW", "Ván cờ AI 3D kết thúc với kết quả hòa.");
            return;
        }

        if (result.is_check) {
            setAI3DStatus("Bạn đang bị chiếu!");
        } else {
            setAI3DStatus("Lượt của bạn.");
        }
    } catch (error) {
        console.error("Lỗi AI 3D:", error);
        setAI3DStatus("Lỗi kết nối AI.");
    }
}

/* ÁP DỤNG NƯỚC ĐI 3D */
async function applyMove3D(fromSquare, toSquare) {
    const movingPiece = piecesBySquare.get(fromSquare);

    if (!movingPiece) {
        await createPiecesFromFen(fen);
        return;
    }

    const targetPiece = piecesBySquare.get(toSquare);

    if (targetPiece && targetPiece.userData.color !== movingPiece.userData.color) {
        await animateCapture(targetPiece);
    }

    piecesBySquare.delete(fromSquare);
    piecesBySquare.set(toSquare, movingPiece);

    movingPiece.userData.square = toSquare;

    await animatePieceMove(movingPiece, fromSquare, toSquare);
}

/* ANIMATION ĂN QUÂN */
function animateCapture(piece) {
    return new Promise(function (resolve) {
        const square = piece.userData.square;
        piecesBySquare.delete(square);

        const startScale = piece.scale.clone();
        const duration = 320;
        const startTime = performance.now();

        function update(now) {
            const t = Math.min((now - startTime) / duration, 1);
            const s = 1 - t;

            piece.scale.set(startScale.x * s, startScale.y * s, startScale.z * s);
            piece.position.y = 0.05 + t * 1.2;

            if (t < 1) {
                requestAnimationFrame(update);
            } else {
                pieceGroup.remove(piece);
                resolve();
            }
        }

        requestAnimationFrame(update);
    });
}

/* ANIMATION DI CHUYỂN */
function animatePieceMove(piece, fromSquare, toSquare) {
    return new Promise(function (resolve) {
        isAnimating = true;

        const fromPos = piece.position.clone();
        const toPos = squareToPosition(toSquare);
        toPos.y = 0.05;

        const duration = 620;
        const startTime = performance.now();

        setAI3DStatus("Đang di chuyển " + fromSquare + " → " + toSquare + "...");

        function update(now) {
            const rawT = Math.min((now - startTime) / duration, 1);
            const t = rawT * rawT * (3 - 2 * rawT);

            piece.position.x = THREE.MathUtils.lerp(fromPos.x, toPos.x, t);
            piece.position.z = THREE.MathUtils.lerp(fromPos.z, toPos.z, t);
            piece.position.y = THREE.MathUtils.lerp(fromPos.y, toPos.y, t) + Math.sin(t * Math.PI) * 0.75;

            if (rawT < 1) {
                requestAnimationFrame(update);
            } else {
                piece.position.copy(toPos);
                isAnimating = false;
                resolve();
            }
        }

        requestAnimationFrame(update);
    });
}

/* CẬP NHẬT LỊCH SỬ NƯỚC ĐI */
function updateAI3DHistory() {
    if (!ai3dMoveHistory) {
        return;
    }

    ai3dMoveHistory.innerHTML = "";

    if (moveHistoryDisplay.length === 0) {
        ai3dMoveHistory.textContent = "Chưa có nước đi.";
        return;
    }

    moveHistoryDisplay.forEach(function (item, index) {
        const row = document.createElement("div");
        row.className = "ai3d-history-row " + item.type;

        const no = document.createElement("div");
        no.className = "ai3d-history-index";
        no.textContent = index + 1;

        const main = document.createElement("div");
        main.className = "ai3d-history-main";

        const side = document.createElement("span");
        side.className = "ai3d-history-side";
        side.textContent = item.side;

        const move = document.createElement("span");
        move.className = "ai3d-history-move";
        move.textContent = item.move;

        main.appendChild(side);
        main.appendChild(move);

        row.appendChild(no);
        row.appendChild(main);

        ai3dMoveHistory.appendChild(row);
    });

    ai3dMoveHistory.scrollTop = ai3dMoveHistory.scrollHeight;
}

/* THÊM LỊCH SỬ */
function addAI3DHistory(side, move, type) {
    moveHistoryDisplay.push({
        side: side,
        move: move,
        type: type
    });

    moveHistoryForSave.push(move);
    updateAI3DHistory();
}

/* HIỆN MODAL KẾT QUẢ */
function showAI3DResult(result, customMessage = "") {
    let title = "Trận đấu kết thúc";
    let message = customMessage || "Kết quả đã được lưu.";
    let score = "";
    let icon = "🏆";

    if (result === "WHITE_WIN") {
        title = "Chúc mừng!";
        message = customMessage || "Bạn đã chiến thắng AI 3D!";
        score = "+15 điểm";
        icon = "🏆";
    } else if (result === "BLACK_WIN") {
        title = "Bạn đã thua!";
        message = customMessage || "AI 3D đã chiến thắng ván này.";
        score = "-10 điểm";
        icon = "😓";
    } else {
        title = "Ván cờ hòa!";
        message = customMessage || "Bạn và AI 3D đã hòa.";
        score = "+5 điểm";
        icon = "🤝";
    }

    if (typeof showGameResultModal === "function") {
        showGameResultModal({
            icon: icon,
            title: title,
            message: message,
            score: score
        });
    } else {
        alert(title + "\n" + message + "\n" + score);
    }
}

/* LƯU LỊCH SỬ VÁN AI 3D VÀ CỘNG ĐIỂM */
function saveAI3DHistory(result, customMessage = "") {
    if (historySaved) {
        return;
    }

    historySaved = true;

    fetch("/Play/SaveGameHistory", {
        method: "POST",
        headers: {
            "Content-Type": "application/json"
        },
        body: JSON.stringify({
            modeName: "Chơi Với AI 3D",
            modeType: "BOT",
            botName: "Bot 3D " + ai3dLevel,
            result: result,
            fen: fen,
            moves: moveHistoryForSave
        })
    })
        .then(function (res) {
            return res.json();
        })
        .then(function (data) {
            if (data.success) {
                showAI3DResult(result, customMessage);
            } else {
                historySaved = false;
                alert(data.message || "Không lưu được lịch sử AI 3D.");
            }
        })
        .catch(function (error) {
            historySaved = false;
            console.error("Lỗi lưu AI 3D:", error);
            alert("Lỗi kết nối khi lưu lịch sử AI 3D.");
        });
}

/* BẮT ĐẦU GAME */
async function startAI3DGame() {
    ai3dLevel = ai3dLevelSelect ? ai3dLevelSelect.value : "medium";
    ai3dPlayerColor = ai3dColorSelect ? ai3dColorSelect.value : "white";

    if (ai3dPlayerColor === "random") {
        ai3dPlayerColor = Math.random() > 0.5 ? "white" : "black";
    }

    ai3dAIColor = ai3dPlayerColor === "white" ? "black" : "white";

    fen = "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1";
    fenHistory = [fen];
    lastAiMove = null;
    selectedPiece = null;
    isBusy = false;
    gameOver = false;
    historySaved = false;
    moveHistoryForSave = [];
    moveHistoryDisplay = [];
    legalMoveSquares = [];
    updateAI3DHistory();
    pendingPromotionMove = "";
    hideAI3DPromotionModal();

    ai = new ChessAI(ai3dLevel);

    if (ai3dSideText) {
        ai3dSideText.textContent =
            "Bạn cầm " + (ai3dPlayerColor === "white" ? "Trắng" : "Đen") +
            " | AI: " + ai3dLevel;
    }

    setAI3DStatus("Đang tải bàn cờ 3D và model quân cờ...");
    await initScene();

    if (ai3dPlayerColor === "white") {
        setAI3DStatus("Bàn cờ 3D đã sẵn sàng. Lượt của bạn.");
    } else {
        setAI3DStatus("Bạn cầm Đen. AI đi trước...");
        isBusy = true;
        await aiMove();
        isBusy = false;
    }
}

/* LOOP */
function animate() {
    if (!renderer || !scene || !camera) {
        return;
    }

    animationId = requestAnimationFrame(animate);

    if (controls) {
        controls.update();
    }

    renderer.render(scene, camera);
}

/* RESIZE */
function handleResize() {
    if (!renderer || !camera || !ai3dSceneContainer) {
        return;
    }

    const width = ai3dSceneContainer.clientWidth;
    const height = ai3dSceneContainer.clientHeight;

    camera.aspect = width / height;
    camera.updateProjectionMatrix();

    renderer.setSize(width, height);
}

/* GẮN SỰ KIỆN */
if (ai3dStartBtn) {
    ai3dStartBtn.addEventListener("click", startAI3DGame);
}

/* VÁN MỚI */
if (ai3dNewGameBtn) {
    ai3dNewGameBtn.addEventListener("click", function () {
        startAI3DGame();
    });
}

/* ĐẦU HÀNG */
if (ai3dResignBtn) {
    ai3dResignBtn.addEventListener("click", function () {
        if (gameOver) {
            setAI3DStatus("Ván cờ đã kết thúc.");
            return;
        }

        gameOver = true;
        isBusy = true;
        selectedPiece = null;
        resetSquareHighlights();

        addAI3DHistory("Bạn", "Đầu hàng", "player");

        setAI3DStatus("Bạn đã đầu hàng. AI 3D thắng.");
        saveAI3DHistory("BLACK_WIN", "Bạn đã đầu hàng. AI 3D thắng.");
    });
}

window.addEventListener("resize", handleResize);

console.log("BanCo3D.js đã load - bản sửa click, xoay bàn cờ, model Blender và gợi ý nước đi.");