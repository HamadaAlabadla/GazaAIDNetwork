function EditDivision(divisionId) {
    $.ajax({
        url: '/Divisions/GetDivision', // Controller Action to Get Division Data
        type: 'GET',
        data: { id: divisionId },
        success: function (response) {
            if (response.success) {
                $("#EditDivisionModal #Name").val(response.data.name);
                $("#EditDivisionModal #DivisionId").val(response.data.id);
                $("#EditDivisionModal #IsDelete").val(response.data.isDelete);
                $("#EditDivisionModal").modal("show");
            } else {
                Swal.fire({
                    text: "حدث خطأ أثناء جلب البيانات!",
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
            }); }
    });
}




$("#EditDivisionForm").submit(function (e) {
    e.preventDefault();
    let formData = new FormData();
    let form = $(this);
    formData.append("Name", form.find("#Name").val());
    formData.append("Id", form.find("#DivisionId").val());
    formData.append("IsDelete", form.find("#IsDelete").val());

    $.ajax({
        url: '/Divisions/Edit', // Update with your endpoint
        type: "Post",
        datatype: "json",
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
                    location.reload(); // Refresh page or reload datatable
                });
            }
            else {
                Swal.fire({
                    text: response.message || "حدث خطأ أثناء التعديل!",
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
});
