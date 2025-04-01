function Accept(familyId) {
    familyId = familyId.toString();
    Swal.fire({
        title: "هل أنت متأكد؟",
        text: "سيتم إعتماد طلب الإغاثة",
        icon: "warning",
        showCancelButton: true,
        confirmButtonText: "نعم، اعتمد!",
        cancelButtonText: "إلغاء",
        customClass: {
            confirmButton: "btn btn-success",
            cancelButton: "btn btn-secondary"
        }
    }).then((result) => {
        if (result.isConfirmed) {
            var selectedValue = document.getElementById("FinancialSituation").value;
            console.log(selectedValue);
            $.ajax({
                url: "/Families/Accept", // Update with your actual endpoint
                type: "POST",
                data: { id: familyId, financialSituation: selectedValue },
                success: function (response) {
                    if (response.success) {
                        Swal.fire({
                            text: response.message,
                            icon: "success",
                            confirmButtonText: "موافق",
                            customClass: { confirmButton: "btn btn-primary" }
                        }).then(() => {
                            window.location.href = '/families/ReliefRequests?familyStatus=5';
                    });
                    } else {
                        Swal.fire({
                            text: response.message || "حدث خطأ أثناء اعتماد الطلب!",
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



function Reject(familyId) {
    familyId = familyId.toString();

    Swal.fire({
        title: "هل أنت متأكد؟",
        text: "يرجى إدخال سبب الرفض قبل المتابعة",
        icon: "warning",
        input: "textarea",
        inputPlaceholder: "أدخل سبب الرفض هنا...",
        inputAttributes: {
            required: true
        },
        showCancelButton: true,
        confirmButtonText: "رفض الطلب",
        cancelButtonText: "إلغاء",
        customClass: {
            confirmButton: "btn btn-danger",
            cancelButton: "btn btn-secondary"
        },
        preConfirm: (reason) => {
            if (!reason) {
                Swal.showValidationMessage("يجب إدخال سبب الرفض!");
            }
            return reason;
        }
    }).then((result) => {
        if (result.isConfirmed) {
            const rejectionReason = result.value; // Get the entered reason

            $.ajax({
                url: "/Families/Rejected", // Update with your actual endpoint
                type: "POST",
                data: { id: familyId, message: rejectionReason },
                success: function (response) {
                    if (response.success) {
                        Swal.fire({
                            text: response.message,
                            icon: "success",
                            confirmButtonText: "موافق",
                            customClass: { confirmButton: "btn btn-primary" }
                        }).then(() => {
                            window.location.href = '/families/ReliefRequests?familyStatus=5';
                        });
                    } else {
                        Swal.fire({
                            text: response.message || "حدث خطأ أثناء رفض الطلب!",
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

