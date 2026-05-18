const container = document.querySelector(".container");
const registerBtn = document.getElementById("registerBtn");
const loginBtn = document.getElementById("loginBtn");

registerBtn.onclick = () => {
    container.classList.add("active");
};

loginBtn.onclick = () => {
    container.classList.remove("active");
};

// Toggle hiển thị mật khẩu
const togglePassword1 = document.getElementById('togglePassword1');
const togglePassword2 = document.getElementById('togglePassword2');
const toggleConfirmPassword = document.getElementById('toggleConfirmPassword');

const passwordInput1 = document.getElementById('password1');
const passwordInput2 = document.getElementById('password2');
const confirmPasswordInput = document.getElementById('confirmPassword');
const matchMessage = document.getElementById('matchMessage');

if (togglePassword1) {
    togglePassword1.addEventListener('click', () => {
        const type = passwordInput1.getAttribute('type') === 'password' ? 'text' : 'password';
        passwordInput1.setAttribute('type', type);
        togglePassword1.classList.toggle('active');
        togglePassword1.classList.toggle('fa-eye');
        togglePassword1.classList.toggle('fa-eye-slash');
    });
}

if (togglePassword2) {
    togglePassword2.addEventListener('click', () => {
        const type = passwordInput2.getAttribute('type') === 'password' ? 'text' : 'password';
        passwordInput2.setAttribute('type', type);
        togglePassword2.classList.toggle('active');
        togglePassword2.classList.toggle('fa-eye');
        togglePassword2.classList.toggle('fa-eye-slash');
    });
}

if (toggleConfirmPassword) {
    toggleConfirmPassword.addEventListener('click', () => {
        const type = confirmPasswordInput.getAttribute('type') === 'password' ? 'text' : 'password';
        confirmPasswordInput.setAttribute('type', type);
        toggleConfirmPassword.classList.toggle('active');
        toggleConfirmPassword.classList.toggle('fa-eye');
        toggleConfirmPassword.classList.toggle('fa-eye-slash');
    });
}

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
            matchMessage.textContent = '✗ Mật khẩu không trùng khớp';
            matchMessage.classList.remove('success');
            matchMessage.classList.add('error');
        }
    });
}