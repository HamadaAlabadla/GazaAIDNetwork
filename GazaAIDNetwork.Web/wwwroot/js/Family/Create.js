document.getElementById("GenderForHusband").addEventListener("change", function () {
    var husbandStatus = this.value;
    var maritalStatusDropdown = document.getElementById("MaritalStatus");
    var genderForWifeOptions = document.getElementById("GenderForWife").getElementsByTagName("option");
    var maritalOptions = maritalStatusDropdown.getElementsByTagName("option");
    maritalStatusDropdown.value = "";
    if (husbandStatus == "1") {
        genderForWifeOptions[2].style.display = "none";
        genderForWifeOptions[1].style.display = "block";
        // Show only specific options when 'أنثى' is selected (value 1)
        for (var i = 0; i < maritalOptions.length; i++) {
            if (maritalOptions[i].value === "0" || maritalOptions[i].value === "5") {
                maritalOptions[i].style.display = "none";
            } else {
                maritalOptions[i].style.display = "block";
            }
        }
    } else {
        genderForWifeOptions[2].style.display = "block";
        genderForWifeOptions[1].style.display = "none";
        // Show all options when 'ذكر' is selected or no value is selected
        for (var i = 0; i < maritalOptions.length; i++) {
            if (maritalOptions[i].value === "0" || maritalOptions[i].value === "5") {
                maritalOptions[i].style.display = "block";
            } else {
                maritalOptions[i].style.display = "none";
            }
        }
    }
}); 


document.getElementById("MaritalStatus").addEventListener("change", function () {
    var maritalStatus = this.value;
    var wifeCard = document.getElementById("wifeCard");

    if (maritalStatus == "1" || maritalStatus == "2"|| maritalStatus == "3") {

        wifeCard.style.display = "none";
    } else {
        wifeCard.style.display = "block";
    }
});


document.getElementById("IsDisplaced").addEventListener("change", function () {
    var displaceCard = document.getElementById("dispaceCard");
    displaceCard.style.display = this.checked ? "block" : "none";
});
document.getElementById("IsDisability").addEventListener("change", function () {
    var disabilityCard = document.getElementById("disabilityCard");
    disabilityCard.style.display = this.checked ? "block" : "none";
});
document.getElementById("IsDisease").addEventListener("change", function () {
    var diseaseCard = document.getElementById("diseaseCard");
    diseaseCard.style.display = this.checked ? "block" : "none";
});

// Ensure the state is correct on page load (e.g., for editing forms)
window.addEventListener("load", function () {
    var IsDisplaced = document.getElementById("IsDisplaced");
    var displaceCard = document.getElementById("dispaceCard");
    displaceCard.style.display = IsDisplaced.checked ? "block" : "none";

    var IsDisability = document.getElementById("IsDisability");
    var disabilityCard = document.getElementById("disabilityCard");
    disabilityCard.style.display = IsDisability.checked ? "block" : "none";

    var IsDisease = document.getElementById("IsDisease");
    var diseaseCard = document.getElementById("diseaseCard");
    diseaseCard.style.display = IsDisease.checked ? "block" : "none";
});


$(document).ready(function () {
    if ($('#RepresentativeId').length) {
        $("select[name='RepresentativeId']").on("change", function () {
            $(this).attr("data-selected", $(this).val());
        });
        $.ajax({
            url: "/User/GetRepresentatives",
            type: "GET",
            timeout: 5000, // Set timeout to 5 seconds (5000ms)
            success: function (data) {
                var select = $("select[name='RepresentativeId']");
                var selectedValue = $("#RepresentativeId").data("selected"); // Store the value from userDto

                select.empty().append('<option value="">اختر المندوب ...</option>');

                $.each(data, function (index, item) {
                    select.append(`<option value="${item.id}">${item.fullName}</option>`);
                });

                // Delay setting the selected value to ensure options are populated
                setTimeout(function () {
                    if (selectedValue) {
                        select.val(selectedValue);
                    }
                }, 1000); // Adjust the timeout if necessary
            },
            error: function () {
                console.error("فشل في تحميل قائمة المندوبين");
            }
        });
    }
});