def classify_move(loss, delta=0, is_best=False, is_checkmate=False):
    """
    Chấm nước đi theo độ lệch so với nước tốt nhất AI tìm được.

    loss:
        0 nghĩa là nước đang xét gần như tốt nhất.
        loss càng lớn nghĩa là nước đó càng kém nước tốt nhất.

    delta:
        điểm thay đổi trực tiếp sau nước đi.
        delta > 0: tốt hơn cho bên vừa đi.
        delta < 0: xấu hơn cho bên vừa đi.
    """

    if is_checkmate:
        return "BEST", "Nước chiếu hết. Đây là nước kết thúc ván rất mạnh."

    if is_best or loss <= 10:
        return "BEST", "Nước đi gần như tốt nhất trong vị trí này."

    if loss <= 40:
        return "GOOD", "Nước đi tốt, chỉ kém phương án mạnh nhất một chút."

    if loss <= 100:
        return "INACCURACY", "Nước đi chưa chính xác. Có phương án tốt hơn để giữ hoặc tăng lợi thế."

    if loss <= 250:
        return "MISTAKE", "Nước đi sai đáng kể. Bạn đã bỏ lỡ một phương án quan trọng hơn."

    return "BLUNDER", "Nước đi lỗi nặng. Nước này làm mất nhiều lợi thế hoặc có thể mất quân quan trọng."


def build_move_comment(label, san, best_san, loss, delta, is_capture, gives_check, is_checkmate):
    notes = []

    if is_checkmate:
        notes.append("Nước này tạo chiếu hết.")
    elif gives_check:
        notes.append("Nước này có tạo thế chiếu.")

    if is_capture:
        notes.append("Đây là nước ăn quân.")

    if best_san and san != best_san:
        notes.append(f"AI gợi ý nước tốt hơn là {best_san}.")

    if label == "BEST":
        notes.append("Bạn đã chọn một phương án rất mạnh trong vị trí này.")
    elif label == "GOOD":
        notes.append("Nước này vẫn ổn, nhưng chưa phải lựa chọn sắc bén nhất.")
    elif label == "INACCURACY":
        notes.append("Nước này hơi thiếu chính xác, nên xem lại ý tưởng chiến thuật ở vị trí này.")
    elif label == "MISTAKE":
        notes.append("Nước này làm thế cờ yếu đi rõ rệt. Nên kiểm tra quân đang bị tấn công trước khi đi.")
    elif label == "BLUNDER":
        notes.append("Đây là lỗi lớn. Cần kiểm tra mất quân, chiếu hết, hoặc nước phản công của đối thủ.")

    notes.append(f"Độ lệch so với nước tốt nhất: {round(loss, 2)}.")
    notes.append(f"Thay đổi trực tiếp sau nước đi: {round(delta, 2)}.")

    return " ".join(notes)