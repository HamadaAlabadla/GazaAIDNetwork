function ReactivateUser(userId) {
    userId = userId.toString();
    Swal.fire({
        title: "إعادة التفعيل؟",
        text: "هل تريد إعادة تفعيل هذا المستخدم؟",
        icon: "question",
        showCancelButton: true,
        confirmButtonText: "نعم، إعادة التفعيل!",
        cancelButtonText: "إلغاء",
        customClass: {
            confirmButton: "btn btn-success",
            cancelButton: "btn btn-secondary"
        }
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: "/User/Activate",
                type: 'POST',
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
                            text: response.message || "حدث خطأ أثناء إعادة التفعيل!",
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
