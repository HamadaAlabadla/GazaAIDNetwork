function Start(projectId) {
    projectId = projectId.toString();

    Swal.fire({
        title: "اختر تاريخ الانتهاء",
        html: `<input type="datetime-local" id="executionDateTime" class="swal2-input">`,
        icon: "info",
        showCancelButton: true,
        confirmButtonText: "التالي",
        cancelButtonText: "إلغاء",
        customClass: {
            confirmButton: "btn btn-primary",
            cancelButton: "btn btn-secondary"
        },
        preConfirm: () => {
            const selectedDateTime = document.getElementById("executionDateTime").value;
            if (!selectedDateTime) {
                Swal.showValidationMessage("يرجى اختيار تاريخ ووقت الانتهاء!");
                return false;
            }
            if (new Date(selectedDateTime) < new Date()) {
                Swal.showValidationMessage("لا يمكنك اختيار وقت في الماضي!");
                return false;
            }
            return selectedDateTime;
        }
    }).then((result) => {
        if (result.isConfirmed) {
            const selectedDateTime = result.value;

            Swal.fire({
                title: "هل أنت متأكد؟",
                text: "سيتم اعتماد المشروع وإبلاغ المستفيدين",
                icon: "warning",
                showCancelButton: true,
                confirmButtonText: "نعم، اعتمد!",
                cancelButtonText: "إلغاء",
                customClass: {
                    confirmButton: "btn btn-success",
                    cancelButton: "btn btn-secondary"
                }
            }).then((finalResult) => {
                if (finalResult.isConfirmed) {
                    $.ajax({
                        url: "/ProjectAid/Start",
                        type: "POST",
                        data: { id: projectId, EndDate: selectedDateTime },
                        success: function (response) {
                            if (response.success) {
                                Swal.fire({
                                    text: response.message,
                                    icon: "success",
                                    confirmButtonText: "موافق",
                                    customClass: { confirmButton: "btn btn-primary" }
                                }).then(() => {
                                    location.reload();
                                });
                            } else {
                                Swal.fire({
                                    text: response.message || "حدث خطأ أثناء اعتماد المشروع!",
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
    });
}
