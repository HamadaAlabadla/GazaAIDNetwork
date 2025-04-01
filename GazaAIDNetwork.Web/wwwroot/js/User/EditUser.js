function EditUser(userId) {
    var select = $("#kt_modal_edit_user_form #DivisionId");

    if ($('#kt_modal_add_user_form #DivisionId').length) {
        // تنفيذ الطلب الأول ثم الطلب الثاني بعد اكتماله
        $.ajax({
            url: '/Divisions/GetAllDivisions',
            type: 'GET',
            dataType: 'json'
        }).done(function (data) {
            if (data.result && data.result.length > 0) {
                // تفريغ الخيارات القديمة وإضافة الجديدة
                select.empty().append('<option value="">اختر الشعبة</option>');

                data.result.forEach(function (division) {
                    select.append(
                        $('<option></option>').val(division.id).text(division.name)
                    );
                });
            }

            // بعد اكتمال جلب البيانات، تنفيذ الطلب الثاني
            return $.ajax({
                url: '/User/GetUser',
                type: 'GET',
                data: { id: userId }
            }).done(function (response) {
                if (response.success) {
                    $("#EditUserModal #Id").val(response.data.id);
                    $("#EditUserModal #FullName").val(response.data.fullName);
                    $("#EditUserModal #IdNumber").val(response.data.idNumber);
                    $("#EditUserModal #PhoneNumber").val(response.data.phoneNumber);

                    // تعيين القيمة في القائمة
                    select.val(response.data.divisionId);

                    // التأكد من أن الخيار موجود وتحديده إذا لم يتم اختياره تلقائيًا
                    if (select.find("option[value='" + response.data.divisionId + "']").length) {
                        select.find("option[value='" + response.data.divisionId + "']").prop("selected", true);
                    }

                    // تشغيل حدث التغيير إذا كنت تستخدم مكتبة مثل Select2
                    select.trigger('change');

                    // Assume response.data.roles is an array of role values, like [10, 11, 12]
                    $.each(response.data.roles, function (index, role) {
                        $("#EditUserModal input[type='checkbox'][value='" + role + "']").prop("checked", true);
                    });


                    // فتح المودال
                    $("#EditUserModal").modal("show");
                } else {
                    Swal.fire({
                        text: "حدث خطأ أثناء جلب البيانات!",
                        icon: "error",
                        confirmButtonText: "حسنًا",
                        customClass: { confirmButton: "btn btn-danger" }
                    });
                }
            });

        }).fail(function () {
            Swal.fire({
                text: "حدث خطأ أثناء الاتصال بالخادم!",
                icon: "error",
                confirmButtonText: "حسنًا",
                customClass: { confirmButton: "btn btn-danger" }
            });
        });
    } else {
        $.ajax({
            url: '/User/GetUser',
            type: 'GET',
            data: { id: userId }
        }).done(function (response) {
            if (response.success) {
                $("#EditUserModal #Id").val(response.data.id);
                $("#EditUserModal #FullName").val(response.data.fullName);
                $("#EditUserModal #IdNumber").val(response.data.idNumber);
                $("#EditUserModal #PhoneNumber").val(response.data.phoneNumber);
                $("#EditUserModal #DivisionId").val(response.data.divisionId);

                // Assume response.data.roles is an array of role values, like [10, 11, 12]
                $.each(response.data.roles, function (index, role) {
                    $("#EditUserModal input[type='checkbox'][value='" + role + "']").prop("checked", true);
                });

                // فتح المودال
                $("#EditUserModal").modal("show");
            } else {
                Swal.fire({
                    text: "حدث خطأ أثناء جلب البيانات!",
                    icon: "error",
                    confirmButtonText: "حسنًا",
                    customClass: { confirmButton: "btn btn-danger" }
                });
            }
        });
    }
}
$(document).ready(function () {

    let editModal = $("#EditUserModal");
    let editForm = $("#kt_modal_edit_user_form");
    let submitButtonEdit = $("[data-kt-edituser-modal-action='submit']");
    submitButtonEdit.find(".indicator-label").show();
    submitButtonEdit.find(".indicator-progress").hide();

    function resetForm() {
        editForm[0].reset(); // Reset form fields
        editForm.find(".is-invalid").removeClass("is-invalid"); // Remove validation errors
        editForm.find(".invalid-feedback").remove(); // Remove error messages
    }
    // Close Modal
    $("[data-kt-edituser-modal-action='close']").on("click", function () {
        resetForm();
        editModal.modal("hide");
    });

    // Discard (Reset Form & Close Modal)
    $("[data-kt-edituser-modal-action='cancel']").on("click", function () {
        resetForm();
        editModal.modal("hide");
    });
    submitButtonEdit.on("click", function (e) {
        e.preventDefault();
        let formData = new FormData();

        // Append form data from input fields
        formData.append("Id", editModal.find("#Id").val());
        formData.append("FullName", editModal.find("#FullName").val());
        formData.append("IdNumber", editModal.find("#IdNumber").val());
        formData.append("PhoneNumber", editModal.find("#PhoneNumber").val());
        if ($('#kt_modal_edit_user_form #DivisionId').length) {
            formData.append("DivisionId", editModal.find("#DivisionId").val());
        }
        // Collect all checked checkboxes
        $('input[type="checkbox"]:checked').each(function () {
            formData.append("Roles[]", $(this).val()); // Appending each checked value to FormData
        });
        // Show Loading
        submitButtonEdit.prop("disabled", true);
        submitButtonEdit.find(".indicator-label").hide();
        submitButtonEdit.find(".indicator-progress").show();

        $.ajax({
            url: '/User/Edit', // تحديث حسب نقطة النهاية الفعلية
            type: "POST",
            data: formData,
            contentType: false,
            processData: false,
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
                        text: response.message || "حدث خطأ أثناء التعديل!",
                        icon: "error",
                        confirmButtonText: "حسنًا",
                        customClass: { confirmButton: "btn btn-danger" }
                    }).then(() => {
                        submitButtonEdit.prop("disabled", false);
                        submitButtonEdit.find(".indicator-label").show();
                        submitButtonEdit.find(".indicator-progress").hide();
                    });
                }
            },
            error: function () {
                Swal.fire({
                    text: "حدث خطأ أثناء الاتصال بالخادم!",
                    icon: "error",
                    confirmButtonText: "حسنًا",
                    customClass: { confirmButton: "btn btn-danger" }
                }).then(() => {
                    submitButtonEdit.prop("disabled", false);
                    submitButtonEdit.find(".indicator-label").show();
                    submitButtonEdit.find(".indicator-progress").hide();
                });
            }
        });
    });
});
