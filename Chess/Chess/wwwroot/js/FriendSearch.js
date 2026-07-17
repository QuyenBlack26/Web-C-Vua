/* ========================= */
/* TÌM KIẾM BẠN BÈ LIVE SEARCH */
/* ========================= */

const searchInput = document.getElementById("friendSearchInput");
const clearSearchBtn = document.getElementById("clearSearchBtn");
const searchResults = document.getElementById("searchResults");
const searchLoading = document.getElementById("searchLoading");
const searchEmpty = document.getElementById("searchEmpty");
const resultCount = document.getElementById("resultCount");
const searchHint = document.getElementById("searchHint");

let searchTimer = null;
let lastKeyword = "";

function setLoading(isLoading) {
    if (!searchLoading) {
        return;
    }

    searchLoading.classList.toggle("show", isLoading);
}

function clearResults(message = "Chưa có kết quả. Hãy nhập tên người chơi ở ô tìm kiếm.") {
    if (searchResults) {
        searchResults.innerHTML = "";
    }

    if (searchEmpty) {
        searchEmpty.textContent = message;
        searchEmpty.style.display = "block";
    }

    if (resultCount) {
        resultCount.textContent = "0 người chơi";
    }
}

function getAvatarHtml(user) {
    const avatar = user.Avatar || user.avatar || "";
    const tenDangNhap = user.TenDangNhap || user.tenDangNhap || "U";
    const firstLetter = tenDangNhap.substring(0, 1).toUpperCase();

    if (avatar && avatar.trim() !== "") {
        return `<img src="${avatar}" alt="${tenDangNhap}">`;
    }

    return `<span>${firstLetter}</span>`;
}

function getActionHtml(user) {
    const userId = user.UserID || user.userID || user.userId;
    const quanHe = user.QuanHe || user.quanHe || "NONE";

    if (quanHe === "FRIEND") {
        return `
            <span class="friend-badge friend">Đã là bạn bè</span>
            <a class="btn-schedule" href="/Friend/Schedule?friendId=${userId}">
                Hẹn phòng
            </a>
        `;
    }

    if (quanHe === "SENT") {
        return `
            <span class="friend-badge sent">Đã gửi lời mời</span>
        `;
    }

    if (quanHe === "RECEIVED") {
        return `
            <span class="friend-badge received">Đang có lời mời từ người này</span>
            <a class="btn-schedule" href="/Friend/Index">
                Xem lời mời
            </a>
        `;
    }

    return `
        <form action="/Friend/SendRequest" method="post">
            <input type="hidden" name="receiverId" value="${userId}">
            <button type="submit" class="btn-accept">
                Kết bạn
            </button>
        </form>
    `;
}

function renderResults(users) {
    searchResults.innerHTML = "";

    if (!users || users.length === 0) {
        clearResults("Không tìm thấy người chơi phù hợp.");
        return;
    }

    if (searchEmpty) {
        searchEmpty.style.display = "none";
    }

    if (resultCount) {
        resultCount.textContent = users.length + " người chơi";
    }

    users.forEach(function (user, index) {
        const tenDangNhap = user.TenDangNhap || user.tenDangNhap || "";
        const hoTen = user.HoTen || user.hoTen || "Người chơi Chess Online";
        const gmail = user.Gmail || user.gmail || "";
        const diem = user.Diem || user.diem || 1200;

        const card = document.createElement("div");
        card.className = "friend-item friend-box search-result-item";
        card.style.animationDelay = `${index * 60}ms`;

        card.innerHTML = `
            <div class="friend-user">
                <div class="friend-avatar big">
                    ${getAvatarHtml(user)}
                </div>

                <div>
                    <h3>${tenDangNhap}</h3>
                    <p>${hoTen || "Người chơi Chess Online"}</p>
                    <small>Điểm cao nhất: ${diem}</small>
                    ${gmail ? `<p class="friend-email">${gmail}</p>` : ""}
                </div>
            </div>

            <div class="friend-actions friend-actions-row">
                ${getActionHtml(user)}
            </div>
        `;

        searchResults.appendChild(card);
    });
}

async function searchUsers(keyword) {
    if (keyword.length < 2) {
        setLoading(false);
        clearResults("Nhập ít nhất 2 ký tự để bắt đầu tìm kiếm.");
        return;
    }

    if (keyword === lastKeyword) {
        return;
    }

    lastKeyword = keyword;

    setLoading(true);

    try {
        const response = await fetch(`/Friend/SearchUsers?keyword=${encodeURIComponent(keyword)}`);
        const data = await response.json();

        setLoading(false);

        if (!data.success) {
            clearResults(data.message || "Không thể tìm kiếm người chơi.");
            return;
        }

        renderResults(data.users || []);
    } catch (error) {
        console.error("Lỗi tìm kiếm bạn bè:", error);
        setLoading(false);
        clearResults("Có lỗi khi tìm kiếm. Vui lòng thử lại.");
    }
}

if (searchInput) {
    searchInput.addEventListener("input", function () {
        const keyword = searchInput.value.trim();

        if (clearSearchBtn) {
            clearSearchBtn.classList.toggle("show", keyword.length > 0);
        }

        if (searchHint) {
            if (keyword.length === 0) {
                searchHint.textContent = "Nhập ít nhất 2 ký tự để bắt đầu tìm kiếm.";
            } else if (keyword.length === 1) {
                searchHint.textContent = "Nhập thêm 1 ký tự nữa để tìm kiếm.";
            } else {
                searchHint.textContent = "Đang tìm kiếm theo từ khóa: " + keyword;
            }
        }

        clearTimeout(searchTimer);

        searchTimer = setTimeout(function () {
            searchUsers(keyword);
        }, 350);
    });
}

if (clearSearchBtn) {
    clearSearchBtn.addEventListener("click", function () {
        searchInput.value = "";
        lastKeyword = "";
        clearSearchBtn.classList.remove("show");

        if (searchHint) {
            searchHint.textContent = "Nhập ít nhất 2 ký tự để bắt đầu tìm kiếm.";
        }

        clearResults("Chưa có kết quả. Hãy nhập tên người chơi ở ô tìm kiếm.");
        searchInput.focus();
    });
}