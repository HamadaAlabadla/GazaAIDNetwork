function DeleteFamily(familyId) {
    familyId = familyId.toString();
    Swal.fire({
        title: "هل أنت متأكد؟",
        text: "لن تتمكن من استعادة هذا العنصر!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "نعم، احذفها!",
        cancelButtonText: "إلغاء",
        customClass: {
            confirmButton: "btn btn-danger",
            cancelButton: "btn btn-secondary"
        }
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: "/Families/Delete", // Update with your actual endpoint
                type: "POST",
                data: { id: familyId },
                success: function (response) {
                    if (response.success) {
                        Swal.fire({
                            text: response.message,
                            icon: "success",
                            confirmButtonText: "موافق",
                            customClass: { confirmButton: "btn btn-primary" }
                        }).then(() => {
                            datatable.ajax.reload(null ,false);
                        });
                    } else {
                        Swal.fire({
                            text: response.message || "حدث خطأ أثناء الحذف!",
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
