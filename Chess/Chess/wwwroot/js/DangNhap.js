/* LẤY KHUNG CONTAINER VÀ NÚT CHUYỂN FORM */
const container = document.querySelector(".container");
const registerBtn = document.getElementById("registerBtn");
const loginBtn = document.getElementById("loginBtn");

/* CHUYỂN SANG FORM ĐĂNG KÝ */
registerBtn.onclick = () => {
    container.classList.add("active");
};

/* CHUYỂN SANG FORM ĐĂNG NHẬP */
loginBtn.onclick = () => {
    container.classList.remove("active");
};

// Toggle hiển thị mật khẩu
/* LẤY ICON ẨN HIỆN MẬT KHẨU */
const togglePassword1 = document.getElementById('togglePassword1');
const togglePassword2 = document.getElementById('togglePassword2');
const toggleConfirmPassword = document.getElementById('toggleConfirmPassword');

/* LẤY CÁC Ô NHẬP MẬT KHẨU */
const passwordInput1 = document.getElementById('password1');
const passwordInput2 = document.getElementById('password2');
const confirmPasswordInput = document.getElementById('confirmPassword');
const matchMessage = document.getElementById('matchMessage');

/* ẨN HIỆN MẬT KHẨU ĐĂNG NHẬP */
if (togglePassword1) {
    togglePassword1.addEventListener('click', () => {
        const type = passwordInput1.getAttribute('type') === 'password' ? 'text' : 'password';
        passwordInput1.setAttribute('type', type);
        togglePassword1.classList.toggle('active');
        togglePassword1.classList.toggle('fa-eye');
        togglePassword1.classList.toggle('fa-eye-slash');
    });
}

/* ẨN HIỆN MẬT KHẨU ĐĂNG KÝ */
if (togglePassword2) {
    togglePassword2.addEventListener('click', () => {
        const type = passwordInput2.getAttribute('type') === 'password' ? 'text' : 'password';
        passwordInput2.setAttribute('type', type);
        togglePassword2.classList.toggle('active');
        togglePassword2.classList.toggle('fa-eye');
        togglePassword2.classList.toggle('fa-eye-slash');
    });
}

/* ẨN HIỆN Ô XÁC NHẬN MẬT KHẨU */
if (toggleConfirmPassword) {
    toggleConfirmPassword.addEventListener('click', () => {
        const type = confirmPasswordInput.getAttribute('type') === 'password' ? 'text' : 'password';
        confirmPasswordInput.setAttribute('type', type);
        toggleConfirmPassword.classList.toggle('active');
        toggleConfirmPassword.classList.toggle('fa-eye');
        toggleConfirmPassword.classList.toggle('fa-eye-slash');
    });
}

/* KIỂM TRA MẬT KHẨU XÁC NHẬN CÓ TRÙNG KHÔNG */
if (confirmPasswordInput && matchMessage) {
    confirmPasswordInput.addEventListener('input', () => {
        if (confirmPasswordInput.value === '') {
            matchMessage.textContent = '';
            matchMessage.classList.remove('error', 'success');
        } else if (passwordInput2.value === confirmPasswordInput.value) {
            matchMessage.textContent = '✓ Mật khẩu trùng khớp';
            matchMessage.classList.remove('error');
            matchMessage.classList.add('success');
        } else {
            matchMessage.textContent = 'x Mật khẩu không trùng khớp';
            matchMessage.classList.remove('success');
            matchMessage.classList.add('error');
        }
    });
}

// Nút đăng nhập/đăng ký bằng Facebook, Google, Twitter
/* THÔNG BÁO CHỨC NĂNG MẠNG XÃ HỘI ĐANG PHÁT TRIỂN */
document.querySelectorAll(".social-disabled").forEach(button => {
    button.addEventListener("click", function (e) {
        e.preventDefault();

        const name = this.dataset.name || "mạng xã hội";
        alert(`Chức năng đăng nhập bằng ${name} đang được phát triển.`);
    });
});