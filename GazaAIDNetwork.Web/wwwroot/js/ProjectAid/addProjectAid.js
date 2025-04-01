$(document).ready(function () {
    
    let modal = $("#AddProjectAidModal");
    let form = $("#kt_modal_add_projectAid_form");
    let submitButton = $("[data-kt-projectAid-modal-action='submit']");
    submitButton.find(".indicator-label").show();
    submitButton.find(".indicator-progress").hide();

    function resetForm() {
        form[0].reset(); // Reset form fields
        form.find(".is-invalid").removeClass("is-invalid"); // Remove validation errors
        form.find(".invalid-feedback").remove(); // Remove error messages
    }
    $.ajax({
        url: "/User/GetRepresentatives",
        type: "GET",
        success: function (data) {
            var container = $("#representatives");
            container.empty(); // Clear existing checkboxes

            $.each(data, function (index, item) {
                var checkbox = `
                <div class="form-check checkbox-style checkbox-success mb-20">
                    <input class="form-check-input" type="checkbox" value="${item.id}" id="checkbox-3" name="RepresentativeIds" />
                    <label class="form-check-label" for="checkbox-3">
                      ${item.fullName}</label>
                  </div>
            `;
                container.append(checkbox);
            });
        },
        error: function () {
            console.error("فشل في تحميل قائمة المناديب");
        }
    });

    $(function () {
        $('#datetimepicker1').datetimepicker(
            {
                format: 'YYYY-MM-DD HH:mm', // Format with Date & Time
                showClose: true,
                showClear: true,
                showTodayButton: true,
                icons: {
                    time: "fa fa-clock",
                    date: "fa fa-calendar",
                    up: "fa fa-chevron-up",
                    down: "fa fa-chevron-down",
                    previous: 'fa fa-chevron-left',
                    next: 'fa fa-chevron-right',
                    today: 'fa fa-crosshairs',
                    clear: 'fa fa-trash',
                    close: 'fa fa-times'
                }
            });
        $('#datetimepicker2').datetimepicker(
            {
                format: 'YYYY-MM-DD HH:mm', // Format with Date & Time
                showClose: true,
                showClear: true,
                showTodayButton: true,
                icons: {
                    time: "fa fa-clock",
                    date: "fa fa-calendar",
                    up: "fa fa-chevron-up",
                    down: "fa fa-chevron-down",
                    previous: 'fa fa-chevron-left',
                    next: 'fa fa-chevron-right',
                    today: 'fa fa-crosshairs',
                    clear: 'fa fa-trash',
                    close: 'fa fa-times'
                },
                useCurrent: false //Important! See issue #1075

            });
        $("#datetimepicker1").on("dp.change", function (e) {
            $('#datetimepicker2').data("DateTimePicker").minDate(e.date);
        });
        $("#datetimepicker2").on("dp.change", function (e) {
            $('#datetimepicker1').data("DateTimePicker").maxDate(e.date);
        });
    });
    // Close Modal
    $("[data-kt-projectAid-modal-action='close']").on("click", function () {
        resetForm();
        modal.modal("hide");
    });

    // Discard (Reset Form & Close Modal)
    $("[data-kt-projectAid-modal-action='cancel']").on("click", function () {
        resetForm();
        modal.modal("hide");
    });

    // Submit User Form via AJAX
    submitButton.on("click", function (e) {
        e.preventDefault();

        let btn = $(this);
        let formData = new FormData();

        // Append form data from input fields
        formData.append("Name", document.getElementById("Name").value);
        formData.append("Descreption", document.getElementById("Descreption").value);
        formData.append("Notes", document.getElementById("Notes").value);
        formData.append("DateCreate", document.getElementById("DateCreate").value);
        formData.append("ContinuingUntil", document.getElementById("ContinuingUntil").value);
        formData.append("Quantity", document.getElementById("Quantity").value);
        formData.append("CycleAidId", $('#cycleId').val().toString());
        // Select all checked checkboxes with name="RepresentativeIds"
        var selectedRepresentatives = document.querySelectorAll("input[name='RepresentativeIds']:checked");

        // Loop through selected checkboxes and append values to formData
        selectedRepresentatives.forEach(function (checkbox) {
            formData.append("RepresentativeIds", checkbox.value);
        });

        // Get selected role (assuming radio buttons have the same name "Role")
        // Show Loading
        btn.prop("disabled", true);
        btn.find(".indicator-label").hide();
        btn.find(".indicator-progress").show();

        $.ajax({
            url: '/ProjectAid/Create', // Update with your user creation endpoint
            type: "POST",
            dataType: "json",
            data: formData,
            contentType: false,
            processData: false,
            success: function (response) {
                if (response.success) {
                    Swal.fire({
                        text: response.message,
                        icon: "success",
                        buttonsStyling: false,
                        confirmButtonText: "حسنًا، تم!",
                        customClass: {
                            confirmButton: "btn btn-primary"
                        }
                    }).then(function (result) {
                        if (result.isConfirmed) {
                            resetForm();
                            modal.modal("hide");
                            location.reload(); // Refresh page or reload data dynamically
                        }
                    });
                } else {
                    displayErrors([response.message]);
                }
            },
            error: function () {
                Swal.fire({
                    text: "حدث خطأ أثناء الإرسال!",
                    icon: "error",
                    buttonsStyling: false,
                    confirmButtonText: "موافق",
                    customClass: {
                        confirmButton: "btn btn-danger"
                    }
                });
            },
            complete: function () {
                // Hide Loading
                btn.prop("disabled", false);
                btn.find(".indicator-label").show();
                btn.find(".indicator-progress").hide();
            }
        });
    });

    // Function to Display Errors
    function displayErrors(errors) {
        form.find(".is-invalid").removeClass("is-invalid"); // Remove previous invalid styles
        form.find(".invalid-feedback").remove(); // Remove previous error messages

        if (errors && Object.keys(errors).length > 0) {
            let errorList = "<ul class='text-danger'>";
            Object.keys(errors).forEach(function (key) {
                let input = form.find(`[name='${key}']`);
                input.addClass("is-invalid"); // Highlight the field
                input.after(`<div class="invalid-feedback">${errors[key]}</div>`); // Show error message
                errorList += `<li>${errors[key]}</li>`; // Add to error list
            });
            errorList += "</ul>";

            Swal.fire({
                title: "تحقق من البيانات!",
                html: errorList,
                icon: "error",
                buttonsStyling: false,
                confirmButtonText: "موافق",
                customClass: {
                    confirmButton: "btn btn-danger"
                }
            });
        }
    }
});
