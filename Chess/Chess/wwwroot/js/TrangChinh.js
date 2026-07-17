/* KHỞI TẠO CANVAS CỜ 3D */
function initChessCanvas() {
    const canvas = document.getElementById('chessCanvas');
    if (!canvas) return;

    const ctx = canvas.getContext('2d');
    const rect = canvas.getBoundingClientRect();

    canvas.width = rect.width;
    canvas.height = rect.height;

    const pieces = [
        { x: 50, y: 100, type: '♔', color: '#71b7e6' },
        { x: 100, y: 120, type: '♕', color: '#9b59b6' },
        { x: 150, y: 80, type: '♖', color: '#71b7e6' },
        { x: 200, y: 140, type: '♗', color: '#9b59b6' },
        { x: 250, y: 110, type: '♘', color: '#71b7e6' }
    ];

    let animationFrame = 0;

    /* VẼ BÀN CỜ TRÊN CANVAS */
    function drawChessBoard() {
        const squareSize = 40;
        const boardX = canvas.width / 2 - 160;
        const boardY = canvas.height / 2 - 160;

        for (let i = 0; i < 8; i++) {
            for (let j = 0; j < 8; j++) {
                const x = boardX + i * squareSize;
                const y = boardY + j * squareSize;

                ctx.fillStyle = (i + j) % 2 === 0 ? '#f0f4ff' : '#e0e8ff';
                ctx.fillRect(x, y, squareSize, squareSize);

                ctx.strokeStyle = 'rgba(113, 183, 230, 0.2)';
                ctx.lineWidth = 1;
                ctx.strokeRect(x, y, squareSize, squareSize);
            }
        }
    }

    /* VẼ QUÂN CỜ TRÊN CANVAS */
    function drawPiece(piece, x, y, rotation) {
        ctx.save();
        ctx.translate(x, y);
        ctx.rotate(rotation);

        ctx.font = 'bold 48px Arial';
        ctx.fillStyle = piece.color;
        ctx.textAlign = 'center';
        ctx.textBaseline = 'middle';
        ctx.fillText(piece.type, 0, 0);

        ctx.strokeStyle = 'rgba(0, 0, 0, 0.1)';
        ctx.lineWidth = 2;
        ctx.strokeText(piece.type, 0, 0);

        ctx.restore();
    }

    /* CHẠY ANIMATION QUÂN CỜ */
    function animate() {
        ctx.clearRect(0, 0, canvas.width, canvas.height);

        drawChessBoard();

        pieces.forEach((piece, index) => {
            const wave = Math.sin(animationFrame * 0.05 + index) * 0.3;
            const rotation = Math.sin(animationFrame * 0.02 + index) * 0.2;
            const offsetY = Math.cos(animationFrame * 0.03 + index) * 15;

            drawPiece(piece, piece.x + 50, piece.y + 50 + offsetY, rotation);
        });

        animationFrame++;
        requestAnimationFrame(animate);
    }

    animate();
}

/* XỬ LÝ KHI TRANG CHÍNH LOAD XONG */
document.addEventListener('DOMContentLoaded', function () {
    initChessCanvas();

    // Smooth scroll for anchor links
    /* CUỘN MƯỢT KHI BẤM LINK NỘI BỘ */
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function (e) {
            e.preventDefault();
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                target.scrollIntoView({ behavior: 'smooth' });
            }
        });
    });

    // Animated counter for stats
    /* THEO DÕI PHẦN TỬ XUẤT HIỆN TRÊN MÀN HÌNH */
    const observerOptions = {
        threshold: 0.3
    };

    const observer = new IntersectionObserver(function (entries) {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                entry.target.classList.add('animate');
                observer.unobserve(entry.target);
            }
        });
    }, observerOptions);

    /* GẮN HIỆU ỨNG CHO THẺ TÍNH NĂNG */
    document.querySelectorAll('.feature-card').forEach(card => {
        observer.observe(card);
    });

    /* GẮN HIỆU ỨNG CHO THẺ CHẾ ĐỘ CHƠI */
    document.querySelectorAll('.mode-card-3d').forEach(card => {
        observer.observe(card);
    });

    /* GẮN HIỆU ỨNG CHO THẺ NỔI */
    document.querySelectorAll('.floating-card').forEach(card => {
        observer.observe(card);
    });

    // Parallax effect on scroll
    /* HIỆU ỨNG PARALLAX KHI CUỘN */
    window.addEventListener('scroll', function () {
        const scrolled = window.pageYOffset;
        const parallaxElements = document.querySelectorAll('.hero::before, .hero::after');

        parallaxElements.forEach(el => {
            el.style.transform = `translateY(${scrolled * 0.5}px)`;
        });
    });

    // Hover effect for feature cards
    /* HIỆU ỨNG HOVER CHO THẺ TÍNH NĂNG */
    document.querySelectorAll('.feature-card').forEach(card => {
        card.addEventListener('mouseenter', function () {
            this.style.transform = 'translateY(-15px) scale(1.02)';
        });

        card.addEventListener('mouseleave', function () {
            this.style.transform = 'translateY(0) scale(1)';
        });
    });

    // 3D hover effect for mode cards
    /* HIỆU ỨNG 3D KHI RÊ CHUỘT VÀO THẺ CHẾ ĐỘ */
    document.querySelectorAll('.mode-card-3d').forEach(card => {
        card.addEventListener('mousemove', function (e) {
            const rect = this.getBoundingClientRect();
            const x = e.clientX - rect.left;
            const y = e.clientY - rect.top;

            const rotateX = (y - rect.height / 2) * 0.02;
            const rotateY = (x - rect.width / 2) * -0.02;

            this.style.transform = `perspective(1000px) rotateX(${rotateX}deg) rotateY(${rotateY}deg)`;
        });

        card.addEventListener('mouseleave', function () {
            this.style.transform = 'perspective(1000px) rotateX(0) rotateY(0)';
        });
    });

    // Button ripple effect
    /* HIỆU ỨNG GỢN SÓNG KHI BẤM NÚT */
    document.querySelectorAll('.btn-play, .btn-learn, .btn-premium').forEach(btn => {
        btn.addEventListener('click', function (e) {
            const ripple = document.createElement('span');
            const rect = this.getBoundingClientRect();
            const size = Math.max(rect.width, rect.height);
            const x = e.clientX - rect.left - size / 2;
            const y = e.clientY - rect.top - size / 2;

            ripple.style.width = ripple.style.height = size + 'px';
            ripple.style.left = x + 'px';
            ripple.style.top = y + 'px';
            ripple.classList.add('ripple');

            this.appendChild(ripple);

            setTimeout(() => ripple.remove(), 600);
        });
    });

    // Hamburger menu toggle
    /* MENU MOBILE */
    const hamburger = document.querySelector('.hamburger');
    const navMenu = document.querySelector('.nav-menu');

    if (hamburger) {
        hamburger.addEventListener('click', function () {
            navMenu.style.display = navMenu.style.display === 'flex' ? 'none' : 'flex';
            navMenu.style.position = 'absolute';
            navMenu.style.top = '100%';
            navMenu.style.left = '0';
            navMenu.style.right = '0';
            navMenu.style.backgroundColor = 'white';
            navMenu.style.flexDirection = 'column';
            navMenu.style.padding = '1rem';
            navMenu.style.gap = '1rem';
            navMenu.style.zIndex = '999';
        });
    }

    // Hero title stagger animation
    /* HIỆU ỨNG CHỮ TIÊU ĐỀ HERO */
    const letters = document.querySelectorAll('.letter');
    letters.forEach((letter, index) => {
        letter.style.animation = `bounce 0.6s ease-in-out infinite`;
        letter.style.animationDelay = (index * 0.1) + 's';
    });

    // Scroll progress indicator
    /* TÍNH PHẦN TRĂM CUỘN TRANG */
    window.addEventListener('scroll', function () {
        const scrollHeight = document.documentElement.scrollHeight - window.innerHeight;
        const scrollProgress = (window.pageYOffset / scrollHeight) * 100;

        // Could add progress bar here if needed
    });

    // Add ripple style
    /* THÊM CSS CHO HIỆU ỨNG RIPPLE VÀ ANIMATE */
    const style = document.createElement('style');
    style.textContent = `
        .ripple {
            position: absolute;
            border-radius: 50%;
            background: rgba(255, 255, 255, 0.6);
            transform: scale(0);
            animation: ripple-animation 0.6s ease-out;
            pointer-events: none;
        }

        @keyframes ripple-animation {
            to {
                transform: scale(4);
                opacity: 0;
            }
        }

        .animate {
            animation: fadeInUp 0.8s ease-out !important;
        }

        @keyframes fadeInUp {
            from {
                opacity: 0;
                transform: translateY(20px);
            }
            to {
                opacity: 1;
                transform: translateY(0);
            }
        }
    `;
    document.head.appendChild(style);

    console.log('✨ Modern Chess Homepage Loaded!');
});

// Prevent memory leaks - cleanup on page unload
/* DỌN EVENT KHI RỜI TRANG */
window.addEventListener('beforeunload', function () {
    document.querySelectorAll('*').forEach(el => {
        el.removeEventListener('mouseenter', null);
        el.removeEventListener('mouseleave', null);
        el.removeEventListener('mousemove', null);
        el.removeEventListener('click', null);
    });
});

/* BẬT TẮT DROPDOWN USER */
function toggleUserCard() {
    document.getElementById('userCard').classList.toggle('show');
}

// Click ra ngoài thì đóng
/* CLICK RA NGOÀI THÌ ĐÓNG DROPDOWN USER */
document.addEventListener('click', function (e) {
    const menu = document.querySelector('.user-menu');
    if (menu && !menu.contains(e.target)) {
        document.getElementById('userCard')?.classList.remove('show');
    }
});

/* ========================= */
/* MODAL LIÊN HỆ */
/* ========================= */

function openContactModal() {
    const modal = document.getElementById("contactModalOverlay");

    if (!modal) {
        return;
    }

    modal.classList.add("show");
    document.body.style.overflow = "hidden";
}

function closeContactModal() {
    const modal = document.getElementById("contactModalOverlay");

    if (!modal) {
        return;
    }

    modal.classList.remove("show");
    document.body.style.overflow = "";
}

document.addEventListener("DOMContentLoaded", function () {
    const openContactButtons = document.querySelectorAll(".js-open-contact-modal");
    const closeContactButton = document.getElementById("contactModalClose");
    const contactOverlay = document.getElementById("contactModalOverlay");

    openContactButtons.forEach(function (button) {
        button.addEventListener("click", function (event) {
            event.preventDefault();
            openContactModal();
        });
    });

    if (closeContactButton) {
        closeContactButton.addEventListener("click", function () {
            closeContactModal();
        });
    }

    if (contactOverlay) {
        contactOverlay.addEventListener("click", function (event) {
            if (event.target === contactOverlay) {
                closeContactModal();
            }
        });
    }
});

document.addEventListener("keydown", function (event) {
    if (event.key === "Escape") {
        closeContactModal();
    }
});

/* ========================= */
/* MODAL ĐIỀU KHOẢN */
/* ========================= */

function openTermsModal() {
    const modal = document.getElementById("termsModalOverlay");

    if (!modal) {
        console.log("Không tìm thấy #termsModalOverlay");
        return;
    }

    modal.classList.add("show");
    document.body.style.overflow = "hidden";
}

function closeTermsModal() {
    const modal = document.getElementById("termsModalOverlay");

    if (!modal) {
        return;
    }

    modal.classList.remove("show");
    document.body.style.overflow = "";
}

document.addEventListener("DOMContentLoaded", function () {
    const openTermsButtons = document.querySelectorAll(".js-open-terms-modal");
    const closeTermsButton = document.getElementById("termsModalClose");
    const okTermsButton = document.getElementById("termsModalOk");
    const termsOverlay = document.getElementById("termsModalOverlay");

    openTermsButtons.forEach(function (button) {
        button.addEventListener("click", function (event) {
            event.preventDefault();
            openTermsModal();
        });
    });

    if (closeTermsButton) {
        closeTermsButton.addEventListener("click", function () {
            closeTermsModal();
        });
    }

    if (okTermsButton) {
        okTermsButton.addEventListener("click", function () {
            closeTermsModal();
        });
    }

    if (termsOverlay) {
        termsOverlay.addEventListener("click", function (event) {
            if (event.target === termsOverlay) {
                closeTermsModal();
            }
        });
    }
});

/* ========================= */
/* MODAL BLOG */
/* ========================= */

function openBlogModal() {
    const modal = document.getElementById("blogModalOverlay");

    if (!modal) {
        console.log("Không tìm thấy #blogModalOverlay");
        return;
    }

    modal.classList.add("show");
    document.body.style.overflow = "hidden";
}

function closeBlogModal() {
    const modal = document.getElementById("blogModalOverlay");

    if (!modal) {
        return;
    }

    modal.classList.remove("show");
    document.body.style.overflow = "";
}

document.addEventListener("DOMContentLoaded", function () {
    const openBlogButtons = document.querySelectorAll(".js-open-blog-modal");
    const closeBlogButton = document.getElementById("blogModalClose");
    const okBlogButton = document.getElementById("blogModalOk");
    const blogOverlay = document.getElementById("blogModalOverlay");

    openBlogButtons.forEach(function (button) {
        button.addEventListener("click", function (event) {
            event.preventDefault();
            openBlogModal();
        });
    });

    if (closeBlogButton) {
        closeBlogButton.addEventListener("click", function () {
            closeBlogModal();
        });
    }

    if (okBlogButton) {
        okBlogButton.addEventListener("click", function () {
            closeBlogModal();
        });
    }

    if (blogOverlay) {
        blogOverlay.addEventListener("click", function (event) {
            if (event.target === blogOverlay) {
                closeBlogModal();
            }
        });
    }
});

/* ========================= */
/* MODAL VỀ CHÚNG TÔI */
/* ========================= */

function openAboutModal() {
    const modal = document.getElementById("aboutModalOverlay");

    if (!modal) {
        console.log("Không tìm thấy #aboutModalOverlay");
        return;
    }

    modal.classList.add("show");
    document.body.style.overflow = "hidden";
}

function closeAboutModal() {
    const modal = document.getElementById("aboutModalOverlay");

    if (!modal) {
        return;
    }

    modal.classList.remove("show");
    document.body.style.overflow = "";
}

document.addEventListener("DOMContentLoaded", function () {
    const openAboutButtons = document.querySelectorAll(".js-open-about-modal");
    const closeAboutButton = document.getElementById("aboutModalClose");
    const okAboutButton = document.getElementById("aboutModalOk");
    const aboutOverlay = document.getElementById("aboutModalOverlay");

    openAboutButtons.forEach(function (button) {
        button.addEventListener("click", function (event) {
            event.preventDefault();
            openAboutModal();
        });
    });

    if (closeAboutButton) {
        closeAboutButton.addEventListener("click", function () {
            closeAboutModal();
        });
    }

    if (okAboutButton) {
        okAboutButton.addEventListener("click", function () {
            closeAboutModal();
        });
    }

    if (aboutOverlay) {
        aboutOverlay.addEventListener("click", function (event) {
            if (event.target === aboutOverlay) {
                closeAboutModal();
            }
        });
    }
});