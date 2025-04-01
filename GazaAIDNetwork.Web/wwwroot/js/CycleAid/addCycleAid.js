$(document).ready(function () {
    
    let modal = $("#AddCycleAidModal");
    let form = $("#kt_modal_add_cycleAid_form");
    let submitButton = $("[data-kt-cycleAid-modal-action='submit']");
    submitButton.find(".indicator-label").show();
    submitButton.find(".indicator-progress").hide();

    function resetForm() {
        form[0].reset(); // Reset form fields
        form.find(".is-invalid").removeClass("is-invalid"); // Remove validation errors
        form.find(".invalid-feedback").remove(); // Remove error messages
    }

    // Close Modal
    $("[data-kt-cycleAid-modal-action='close']").on("click", function () {
        resetForm();
        modal.modal("hide");
    });

    // Discard (Reset Form & Close Modal)
    $("[data-kt-cycleAid-modal-action='cancel']").on("click", function () {
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

        // Get selected role (assuming radio buttons have the same name "Role")
        // Show Loading
        btn.prop("disabled", true);
        btn.find(".indicator-label").hide();
        btn.find(".indicator-progress").show();

        $.ajax({
            url: '/CycleAid/Create', // Update with your user creation endpoint
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
                    displayErrors(response.errors);
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
