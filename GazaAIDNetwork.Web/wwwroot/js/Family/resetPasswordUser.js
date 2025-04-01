function ResetPasswordUser(userId) {
    userId = userId.toString();
    Swal.fire({
        title: "هل أنت متأكد؟",
        text: "سيتم إعادة ضبط كلمة المرور!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "نعم!",
        cancelButtonText: "إلغاء",
        customClass: {
            confirmButton: "btn btn-danger",
            cancelButton: "btn btn-secondary"
        }
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: "/User/ResetPasswordByFamilyId", // Update with your actual endpoint
                type: "POST",
                data: { id: userId },
                success: function (response) {
                    if (response.success) {
                        Swal.fire({
                            text: response.message,
                            icon: "success",
                            confirmButtonText: "موافق",
                            customClass: { confirmButton: "btn btn-primary" }
                        }).then(() => {
                            location.reload(); // Refresh page or reload datatable
                        });
                    } else {
                        Swal.fire({
                            text: response.message || "حدث خطأ أثناء إعادة ضبط كلمة المرور!",
                            icon: "error",
                            confirmButtonText: "حسنًا",
                            customClass: { confirmButton: "btn btn-danger" }
                        });
                    }
                },
                error: function () {
                    Swal.fire({
                        text: "حدث خطأ أثناء الاتصال بالخادم!",
                        icon: "error",
                        confirmButtonText: "حسنًا",
                        customClass: { confirmButton: "btn btn-danger" }
                    });
                }
            });
        }
    });
}
