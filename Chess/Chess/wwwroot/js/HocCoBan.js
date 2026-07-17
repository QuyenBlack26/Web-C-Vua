// ===== DỮ LIỆU BÀI HỌC =====
const lessons = {
    pawn: {
        icon: "♙",
        name: "Quân Tốt",
        short: "Quân nhỏ nhất nhưng có nhiều luật đặc biệt.",
        move: "Tốt đi thẳng về phía trước. Nước đầu tiên có thể đi 1 ô hoặc 2 ô nếu không bị chặn.",
        capture: "Tốt ăn chéo 1 ô về phía trước. Tốt không ăn thẳng.",
        special: "Khi Tốt đi đến hàng cuối, nó được phong cấp thành Hậu, Xe, Tượng hoặc Mã.",
        steps: [
            {
                title: "Đi Tốt 1 ô",
                instruction: "Bấm vào ô màu xanh ngay phía trước quân Tốt để đi 1 ô.",
                piece: { row: 6, col: 3, icon: "♙" },
                lessonType: "pawnMove",
                success: "Đúng rồi! Quân Tốt có thể đi thẳng 1 ô."
            },
            {
                title: "Đi Tốt 2 ô ở nước đầu",
                instruction: "Ở nước đầu tiên, Tốt có thể đi 1 ô hoặc 2 ô nếu phía trước không bị chặn.",
                piece: { row: 6, col: 3, icon: "♙" },
                lessonType: "pawnFirstMove",
                success: "Chính xác! Ở nước đầu tiên, Tốt có thể đi 2 ô."
            },
            {
                title: "Tốt ăn chéo",
                instruction: "Tốt không ăn thẳng. Hãy bấm vào quân đen nằm chéo phía trước để ăn.",
                piece: { row: 4, col: 3, icon: "♙" },
                lessonType: "pawnCapture",
                enemies: [
                    { row: 3, col: 2, icon: "♟" },
                    { row: 3, col: 4, icon: "♟" }
                ],
                success: "Đúng! Tốt ăn chéo, không ăn thẳng."
            },
            {
                title: "Tốt không ăn thẳng",
                instruction: "Có quân đen đứng ngay trước mặt. Bấm vào nó để thấy Tốt không được ăn thẳng.",
                piece: { row: 4, col: 3, icon: "♙" },
                lessonType: "pawnWrongForward",
                enemies: [
                    { row: 3, col: 3, icon: "♟" }
                ],
                success: "Tốt không thể ăn quân ở ngay phía trước. Nó chỉ ăn chéo."
            },
            {
                title: "Phong cấp Tốt",
                instruction: "Đưa Tốt lên hàng cuối. Bấm ô màu vàng để phong cấp.",
                piece: { row: 1, col: 3, icon: "♙" },
                lessonType: "pawnPromotion",
                success: "Tuyệt vời! Tốt đến cuối bàn được phong cấp, thường sẽ phong thành Hậu."
            }
        ]
    },

    rook: {
        icon: "♖",
        name: "Quân Xe",
        short: "Xe đi ngang hoặc dọc rất mạnh.",
        move: "Xe đi ngang hoặc dọc bao nhiêu ô cũng được nếu không bị chặn.",
        capture: "Xe ăn quân trên cùng hàng ngang hoặc hàng dọc.",
        special: "Xe có thể tham gia nhập thành cùng với Vua.",
        steps: [
            {
                title: "Xe đi ngang hoặc dọc",
                instruction: "Xe có thể đi ngang hoặc dọc bao nhiêu ô cũng được. Bấm bất kỳ ô xanh nào.",
                piece: { row: 4, col: 3, icon: "♖" },
                lessonType: "rookMove",
                success: "Đúng! Xe đi theo hàng ngang hoặc hàng dọc."
            },
            {
                title: "Xe ăn quân",
                instruction: "Xe ăn quân trên cùng hàng ngang hoặc hàng dọc nếu không bị chặn. Bấm quân đen để ăn.",
                piece: { row: 4, col: 3, icon: "♖" },
                lessonType: "rookCapture",
                enemies: [{ row: 4, col: 6, icon: "♜" }],
                success: "Chính xác! Xe ăn quân trên cùng hàng ngang hoặc dọc."
            }
        ]
    },

    knight: {
        icon: "♘",
        name: "Quân Mã",
        short: "Mã đi hình chữ L và có thể nhảy qua quân khác.",
        move: "Mã đi hình chữ L: 2 ô theo một hướng rồi rẽ 1 ô.",
        capture: "Mã ăn quân ở ô mà nó nhảy tới.",
        special: "Mã là quân duy nhất có thể nhảy qua quân khác.",
        steps: [
            {
                title: "Mã đi hình chữ L",
                instruction: "Mã đi theo hình chữ L. Bấm bất kỳ ô xanh nào để Mã nhảy tới đó.",
                piece: { row: 4, col: 3, icon: "♘" },
                lessonType: "knightMove",
                success: "Đúng! Mã đi theo hình chữ L."
            },
            {
                title: "Mã ăn quân",
                instruction: "Bấm vào quân đen nằm ở ô Mã có thể nhảy tới.",
                piece: { row: 4, col: 3, icon: "♘" },
                lessonType: "knightCapture",
                enemies: [{ row: 2, col: 4, icon: "♞" }],
                success: "Đúng! Mã ăn quân tại ô nó nhảy tới."
            }
        ]
    },

    bishop: {
        icon: "♗",
        name: "Quân Tượng",
        short: "Tượng đi chéo trên cùng màu ô.",
        move: "Tượng đi chéo bao nhiêu ô cũng được nếu không bị chặn.",
        capture: "Tượng ăn quân nằm trên cùng đường chéo.",
        special: "Một Tượng chỉ đi trên ô sáng, Tượng còn lại chỉ đi trên ô tối.",
        steps: [
            {
                title: "Tượng đi chéo",
                instruction: "Tượng đi chéo theo 4 hướng. Bấm bất kỳ ô xanh nào trên đường chéo.",
                piece: { row: 4, col: 3, icon: "♗" },
                lessonType: "bishopMove",
                success: "Đúng! Tượng có thể đi nhiều ô trên đường chéo nếu không bị chặn."
            },
            {
                title: "Tượng ăn chéo",
                instruction: "Tượng ăn quân nằm trên cùng đường chéo. Bấm quân đen để ăn.",
                piece: { row: 4, col: 3, icon: "♗" },
                lessonType: "bishopCapture",
                enemies: [{ row: 2, col: 5, icon: "♝" }],
                success: "Chính xác! Tượng ăn quân theo đường chéo."
            }
        ]
    },

    queen: {
        icon: "♕",
        name: "Quân Hậu",
        short: "Hậu là quân mạnh nhất.",
        move: "Hậu đi ngang, dọc hoặc chéo bao nhiêu ô cũng được nếu không bị chặn.",
        capture: "Hậu ăn quân theo hàng ngang, hàng dọc hoặc đường chéo.",
        special: "Hậu kết hợp sức mạnh của Xe và Tượng.",
        steps: [
            {
                title: "Hậu đi ngang, dọc và chéo",
                instruction: "Hậu mạnh nhất vì đi được như Xe và Tượng. Bấm bất kỳ ô xanh nào.",
                piece: { row: 4, col: 3, icon: "♕" },
                lessonType: "queenMove",
                success: "Đúng! Hậu có thể đi ngang, dọc và chéo."
            },
            {
                title: "Hậu ăn quân",
                instruction: "Hậu ăn quân theo ngang, dọc hoặc chéo. Bấm quân đen để ăn.",
                piece: { row: 4, col: 3, icon: "♕" },
                lessonType: "queenCapture",
                enemies: [{ row: 4, col: 7, icon: "♛" }],
                success: "Đúng! Hậu ăn quân theo ngang, dọc hoặc chéo."
            }
        ]
    },

    king: {
        icon: "♔",
        name: "Quân Vua",
        short: "Vua là quân quan trọng nhất.",
        move: "Vua đi mỗi lần 1 ô theo mọi hướng.",
        capture: "Vua ăn quân ở ô liền kề nếu ô đó an toàn.",
        special: "Vua có nước nhập thành với Xe. Khi nhập thành, Vua đi 2 ô về phía Xe, Xe nhảy qua đứng cạnh Vua.",
        steps: [
            {
                title: "Vua đi 1 ô",
                instruction: "Vua đi mỗi lần 1 ô theo mọi hướng. Bấm bất kỳ ô xanh nào.",
                piece: { row: 4, col: 4, icon: "♔" },
                lessonType: "kingMove",
                success: "Đúng! Vua chỉ đi 1 ô mỗi lần."
            },
            {
                title: "Vua ăn quân",
                instruction: "Bấm vào quân đen ở ô liền kề để Vua ăn.",
                piece: { row: 4, col: 4, icon: "♔" },
                lessonType: "kingCapture",
                enemies: [{ row: 3, col: 4, icon: "♚" }],
                success: "Đúng! Vua có thể ăn quân ở ô bên cạnh nếu ô đó an toàn."
            },
            {
                title: "Nhập thành",
                instruction: "Bấm ô tím bên phải để nhập thành gần. Vua sẽ đi 2 ô về phía Xe.",
                piece: { row: 7, col: 4, icon: "♔" },
                lessonType: "castle",
                allies: [{ row: 7, col: 7, icon: "♖" }],
                success: "Đúng! Khi nhập thành, Vua đi 2 ô và Xe nhảy qua đứng cạnh Vua."
            }
        ]
    }
};

// ===== BIẾN TRẠNG THÁI =====
let currentPiece = "pawn";
let currentStepIndex = 0;
let currentLesson = lessons[currentPiece];
let currentPracticePiece = null;

// ===== LẤY ELEMENT HTML =====
const pieceIcon = document.getElementById("pieceIcon");
const pieceName = document.getElementById("pieceName");
const pieceShort = document.getElementById("pieceShort");
const moveText = document.getElementById("moveText");
const captureText = document.getElementById("captureText");
const specialText = document.getElementById("specialText");
const demoText = document.getElementById("demoText");
const miniBoard = document.getElementById("miniBoard");
const stepText = document.getElementById("stepText");

const prevStepBtn = document.getElementById("prevStepBtn");
const nextStepBtn = document.getElementById("nextStepBtn");
const resetStepBtn = document.getElementById("resetStepBtn");

if (!miniBoard) {
    console.error("Không tìm thấy #miniBoard. Kiểm tra HocCoBan.cshtml");
}

// ===== SỰ KIỆN CHỌN QUÂN =====
document.querySelectorAll(".piece-btn").forEach(button => {
    button.addEventListener("click", function () {
        document.querySelectorAll(".piece-btn").forEach(btn => btn.classList.remove("active"));
        this.classList.add("active");

        currentPiece = this.dataset.piece;
        currentStepIndex = 0;
        loadLesson(currentPiece);
    });
});

// ===== SỰ KIỆN NÚT ĐIỀU HƯỚNG BÀI HỌC =====
prevStepBtn.addEventListener("click", function () {
    if (currentStepIndex > 0) {
        currentStepIndex--;
        renderCurrentStep();
    }
});

nextStepBtn.addEventListener("click", function () {
    if (currentStepIndex < currentLesson.steps.length - 1) {
        currentStepIndex++;
        renderCurrentStep();
    }
});

resetStepBtn.addEventListener("click", function () {
    renderCurrentStep();
});

// ===== TẢI BÀI HỌC THEO QUÂN =====
function loadLesson(pieceKey) {
    currentLesson = lessons[pieceKey];

    pieceIcon.textContent = currentLesson.icon;
    pieceName.textContent = currentLesson.name;
    pieceShort.textContent = currentLesson.short;
    moveText.textContent = currentLesson.move;
    captureText.textContent = currentLesson.capture;
    specialText.textContent = currentLesson.special;

    renderCurrentStep();
}

// ===== HIỂN THỊ BƯỚC HIỆN TẠI =====
function renderCurrentStep() {
    const step = currentLesson.steps[currentStepIndex];

    currentPracticePiece = {
        row: step.piece.row,
        col: step.piece.col,
        icon: step.piece.icon
    };

    refreshTargetsFromCurrentPosition(step);

    const suggested = getSuggestedTarget(step);

    stepText.textContent = `${currentStepIndex + 1}/${currentLesson.steps.length} - ${step.title}`;

    if (suggested) {
        demoText.textContent = `${step.instruction} Gợi ý: hãy thử bấm ô đang nhấp nháy.`;
    } else {
        demoText.textContent = step.instruction;
    }

    drawStepBoard(step, currentPracticePiece, true);
}

// ===== ĐẶT QUÂN CỜ VÀO Ô =====
function putPiece(square, icon) {
    const span = document.createElement("span");
    span.className = "mini-piece-icon";
    span.textContent = icon;
    square.appendChild(span);
}

// ===== VẼ BÀN CỜ THEO BƯỚC =====
function drawStepBoard(step, movedPiece = null, showTargets = true) {
    miniBoard.innerHTML = "";

    for (let row = 0; row < 8; row++) {
        for (let col = 0; col < 8; col++) {
            const square = document.createElement("div");
            square.className = `mini-square ${(row + col) % 2 === 0 ? "light" : "dark"}`;

            square.dataset.row = row;
            square.dataset.col = col;

            const target = showTargets ? getTarget(step, row, col) : null;

            if (target) {
                if (target.type === "move") {
                    square.classList.add("correct-target");
                }

                if (target.type === "capture") {
                    square.classList.add("capture");
                }

                if (target.type === "promotion") {
                    square.classList.add("promotion-target");
                }

                if (target.type === "special") {
                    square.classList.add("special-move");
                }

                if (isSuggestedTarget(step, row, col)) {
                    square.classList.add("recommended-target");
                }
            }

            const enemy = step.enemies?.find(e => e.row === row && e.col === col);
            const ally = step.allies?.find(a => a.row === row && a.col === col);

            const isPieceHere =
                movedPiece &&
                movedPiece.row === row &&
                movedPiece.col === col;

            if (enemy && !isPieceHere) {
                putPiece(square, enemy.icon);
                square.classList.add("enemy-piece");
            }

            if (ally && !isPieceHere) {
                putPiece(square, ally.icon);
                square.classList.add("piece");
            }

            if (isPieceHere) {
                putPiece(square, movedPiece.icon);
                square.classList.add("piece");
            }

            square.addEventListener("click", function () {
                handleSquareClick(step, row, col, square);
            });

            miniBoard.appendChild(square);
        }
    }
}

// ===== XỬ LÝ NGƯỜI HỌC BẤM Ô =====
function handleSquareClick(step, row, col, square) {
    const clickedTarget = getTarget(step, row, col);

    if (!clickedTarget) {
        square.classList.add("wrong-target");
        demoText.textContent = "Chưa đúng. Hãy bấm vào ô được tô màu theo hướng dẫn.";
        return;
    }

    if (clickedTarget.type === "wrong") {
        square.classList.add("wrong-target");
        demoText.textContent = step.success + " Bấm 'Bước tiếp' để học tiếp.";
        return;
    }

    if (clickedTarget.type === "promotion" || step.lessonType === "pawnPromotion") {
        currentPracticePiece = {
            row: row,
            col: col,
            icon: "♕"
        };

        drawStepBoard(step, currentPracticePiece, false);
        demoText.textContent = step.success + " Trong bài này Tốt được phong thành Hậu. Bấm 'Bước tiếp' để học tiếp.";
        return;
    }

    if (clickedTarget.type === "special" || step.lessonType === "castle") {
        drawCastleBoard();
        demoText.textContent = step.success + " Bấm 'Bước tiếp' để học tiếp.";
        return;
    }

    currentPracticePiece = {
        row: row,
        col: col,
        icon: step.piece.icon
    };

    refreshTargetsFromCurrentPosition(step);

    drawStepBoard(step, currentPracticePiece, true);

    const position = getBoardPositionName(row, col);
    demoText.textContent = `${step.success} Bạn đang ở ô ${position}. Các ô xanh là những nước đi tiếp theo từ vị trí mới.`;
}

// ===== VẼ NHẬP THÀNH =====
function drawCastleBoard() {
    miniBoard.innerHTML = "";

    for (let row = 0; row < 8; row++) {
        for (let col = 0; col < 8; col++) {
            const square = document.createElement("div");
            square.className = `mini-square ${(row + col) % 2 === 0 ? "light" : "dark"}`;

            if (row === 7 && col === 6) {
                putPiece(square, "♔");
                square.classList.add("piece");
            }

            if (row === 7 && col === 5) {
                putPiece(square, "♖");
                square.classList.add("piece");
            }

            miniBoard.appendChild(square);
        }
    }
}

// ===== TÍNH LẠI TARGET TỪ VỊ TRÍ HIỆN TẠI =====
function refreshTargetsFromCurrentPosition(step) {
    if (!currentPracticePiece) return;

    const row = currentPracticePiece.row;
    const col = currentPracticePiece.col;

    if (step.lessonType === "rookMove") {
        step.targets = makeLineTargets(row, col);
        return;
    }

    if (step.lessonType === "rookCapture") {
        step.targets = makeCaptureTargetsWithEnemies(row, col, makeLineTargets(row, col), step.enemies);
        return;
    }

    if (step.lessonType === "bishopMove") {
        step.targets = makeDiagonalTargets(row, col);
        return;
    }

    if (step.lessonType === "bishopCapture") {
        step.targets = makeCaptureTargetsWithEnemies(row, col, makeDiagonalTargets(row, col), step.enemies);
        return;
    }

    if (step.lessonType === "queenMove") {
        step.targets = makeQueenTargets(row, col);
        return;
    }

    if (step.lessonType === "queenCapture") {
        step.targets = makeCaptureTargetsWithEnemies(row, col, makeQueenTargets(row, col), step.enemies);
        return;
    }

    if (step.lessonType === "knightMove") {
        step.targets = makeKnightTargets(row, col);
        return;
    }

    if (step.lessonType === "knightCapture") {
        step.targets = makeCaptureTargetsWithEnemies(row, col, makeKnightTargets(row, col), step.enemies);
        return;
    }

    if (step.lessonType === "kingMove") {
        step.targets = makeKingTargets(row, col);
        return;
    }

    if (step.lessonType === "kingCapture") {
        step.targets = makeCaptureTargetsWithEnemies(row, col, makeKingTargets(row, col), step.enemies);
        return;
    }

    if (step.lessonType === "pawnMove") {
        step.targets = makePawnMoveTargets(row, col, false);
        return;
    }

    if (step.lessonType === "pawnFirstMove") {
        step.targets = makePawnMoveTargets(row, col, true);
        return;
    }

    if (step.lessonType === "pawnCapture") {
        step.targets = makePawnCaptureTargets(row, col, step.enemies);
        return;
    }

    if (step.lessonType === "pawnWrongForward") {
        step.targets = [{ row: row - 1, col: col, type: "wrong" }];
        return;
    }

    if (step.lessonType === "pawnPromotion") {
        if (row - 1 >= 0) {
            step.targets = [{ row: row - 1, col: col, type: "promotion" }];
        } else {
            step.targets = [];
        }
        return;
    }

    if (step.lessonType === "castle") {
        step.targets = [{ row: 7, col: 6, type: "special" }];
    }
}

// ===== TÌM Ô TARGET =====
function getTarget(step, row, col) {
    if (!step.targets) return null;
    return step.targets.find(t => t.row === row && t.col === col);
}

// ===== ĐƯỜNG ĐI NGANG / DỌC =====
function makeLineTargets(row, col) {
    const targets = [];

    for (let c = 0; c < 8; c++) {
        if (c !== col) {
            targets.push({ row: row, col: c, type: "move" });
        }
    }

    for (let r = 0; r < 8; r++) {
        if (r !== row) {
            targets.push({ row: r, col: col, type: "move" });
        }
    }

    return targets;
}

// ===== ĐƯỜNG ĐI CHÉO =====
function makeDiagonalTargets(row, col) {
    const targets = [];

    const directions = [
        [-1, -1],
        [-1, 1],
        [1, -1],
        [1, 1]
    ];

    directions.forEach(direction => {
        let r = row + direction[0];
        let c = col + direction[1];

        while (isInsideBoard(r, c)) {
            targets.push({ row: r, col: c, type: "move" });
            r += direction[0];
            c += direction[1];
        }
    });

    return targets;
}

// ===== ĐƯỜNG ĐI CỦA HẬU =====
function makeQueenTargets(row, col) {
    return [
        ...makeLineTargets(row, col),
        ...makeDiagonalTargets(row, col)
    ];
}

// ===== ĐƯỜNG ĐI CỦA MÃ =====
function makeKnightTargets(row, col) {
    const moves = [
        [-2, -1],
        [-2, 1],
        [-1, -2],
        [-1, 2],
        [1, -2],
        [1, 2],
        [2, -1],
        [2, 1]
    ];

    return moves
        .map(move => ({
            row: row + move[0],
            col: col + move[1],
            type: "move"
        }))
        .filter(target => isInsideBoard(target.row, target.col));
}

// ===== ĐƯỜNG ĐI CỦA VUA =====
function makeKingTargets(row, col) {
    const targets = [];

    for (let r = row - 1; r <= row + 1; r++) {
        for (let c = col - 1; c <= col + 1; c++) {
            if (r === row && c === col) continue;

            if (isInsideBoard(r, c)) {
                targets.push({ row: r, col: c, type: "move" });
            }
        }
    }

    return targets;
}

// ===== ĐƯỜNG ĐI CỦA TỐT =====
function makePawnMoveTargets(row, col, allowTwoSteps) {
    const targets = [];

    if (isInsideBoard(row - 1, col)) {
        targets.push({ row: row - 1, col: col, type: "move" });
    }

    if (allowTwoSteps && row === 6 && isInsideBoard(row - 2, col)) {
        targets.push({ row: row - 2, col: col, type: "move" });
    }

    return targets;
}

function makePawnCaptureTargets(row, col, enemies) {
    const targets = [];

    const captureSquares = [
        { row: row - 1, col: col - 1 },
        { row: row - 1, col: col + 1 }
    ];

    captureSquares.forEach(square => {
        if (!isInsideBoard(square.row, square.col)) return;

        const hasEnemy = enemies?.some(e => e.row === square.row && e.col === square.col);

        if (hasEnemy) {
            targets.push({ row: square.row, col: square.col, type: "capture" });
        }
    });

    return targets;
}

// ===== TẠO TARGET ĂN QUÂN =====
function makeCaptureTargetsWithEnemies(pieceRow, pieceCol, moveTargets, enemies) {
    if (!enemies || enemies.length === 0) {
        return moveTargets;
    }

    let targets = [...moveTargets];

    enemies.forEach(enemy => {
        const canReachEnemy = moveTargets.some(t => t.row === enemy.row && t.col === enemy.col);

        if (canReachEnemy) {
            targets = cutAfterTarget(targets, pieceRow, pieceCol, enemy.row, enemy.col);
            targets = replaceTargetType(targets, enemy.row, enemy.col, "capture");
        }
    });

    return targets;
}

// ===== ĐỔI LOẠI Ô TARGET =====
function replaceTargetType(targets, row, col, type) {
    return targets.map(t => {
        if (t.row === row && t.col === col) {
            return { row, col, type };
        }

        return t;
    });
}

// ===== CẮT ĐƯỜNG ĐI SAU QUÂN BỊ ĂN =====
function cutAfterTarget(targets, pieceRow, pieceCol, targetRow, targetCol) {
    return targets.filter(t => {
        const rowDir = Math.sign(targetRow - pieceRow);
        const colDir = Math.sign(targetCol - pieceCol);

        const targetDistance = Math.max(
            Math.abs(targetRow - pieceRow),
            Math.abs(targetCol - pieceCol)
        );

        const squareDistance = Math.max(
            Math.abs(t.row - pieceRow),
            Math.abs(t.col - pieceCol)
        );

        const isSameDirection =
            Math.sign(t.row - pieceRow) === rowDir &&
            Math.sign(t.col - pieceCol) === colDir;

        if (isSameDirection && squareDistance > targetDistance) {
            return false;
        }

        return true;
    });
}

// ===== LẤY Ô GỢI Ý =====
function getSuggestedTarget(step) {
    if (!step.targets || step.targets.length === 0) {
        return null;
    }

    const captureTarget = step.targets.find(t => t.type === "capture");
    if (captureTarget) return captureTarget;

    const specialTarget = step.targets.find(t => t.type === "special" || t.type === "promotion");
    if (specialTarget) return specialTarget;

    const moveTarget = step.targets.find(t => t.type === "move");
    if (moveTarget) return moveTarget;

    return step.targets[0];
}

// ===== KIỂM TRA Ô GỢI Ý =====
function isSuggestedTarget(step, row, col) {
    const suggested = getSuggestedTarget(step);

    if (!suggested) {
        return false;
    }

    return suggested.row === row && suggested.col === col;
}

// ===== ĐỔI TỌA ĐỘ THÀNH TÊN Ô CỜ =====
function getBoardPositionName(row, col) {
    const files = ["a", "b", "c", "d", "e", "f", "g", "h"];
    return files[col] + (8 - row);
}

// ===== KIỂM TRA TRONG BÀN CỜ =====
function isInsideBoard(row, col) {
    return row >= 0 && row < 8 && col >= 0 && col < 8;
}

// ===== KHỞI ĐỘNG BÀI HỌC =====
loadLesson("pawn");