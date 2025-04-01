var table = $("#families");
var datatable;
var filterOrder = []; // Tracks the order in which filters are selected
var filters = {}; // Stores filter values
let selectedItems = new Set(); // Store checked row IDs
var selectAllState = false; // Variable to track the state of "Select All" checkbox

$(document).ready(function () {

    datatable = table.DataTable({
        "rowCallback": function (row, data) {
            $(row).css('height', '15px'); // Adjust as needed
            $(row).css('padding', '0px'); // Adjust as needed
            $(row).css('text-align', 'center !important'); // Adjust as needed
            $(row).css('vertical-align', 'middle !important'); // Adjust as needed
            $(row).children('td').css({
                'padding': '5px',
                'margin': '0px'
            });

            $(row).find('*').css({
                'margin': '0px',
                'padding': '5px'
            });
        },
        "fixedHeader": true, // تفعيل تثبيت الرأس عند التمرير
        "scrollX": true, // Enable horizontal scrolling
        "paging": true, // تفعيل الترقيم
        "pageLength": 10, // عدد العناصر في كل صفحة
        "lengthChange": true, // تمكين تغيير عدد العناصر لكل صفحة
        "processing": true,
        "serverSide": true,
        "filter": true,
        "ajax": {
            "url": "/Families/GeAlltDeletedFamilies",
            "type": "POST",
            "datatype": "json",
            data: function (d) {
                d["excel"] = false; // Send order as a CSV string
                d["filterOrder"] = filterOrder.join(","); // Send order as a CSV string
                for (var key in filters) {
                    d["filter[" + key + "]"] = filters[key];
                }
            }
        },
        "columnDefs": [
            {
                "targets": [0, 7, 8, 9, 10, 11, 12, 13, 14, 16, 17],
                "visible": false,
            },
            {
                "targets": 1, // First column
                "orderable": false, // Disable sorting
                "searchable": false, // Disable searching

            },
            { "className": "text-center", "targets": "_all" }, // Apply to all columns
        ],
        "columns": [
            { "data": "id", "name": "Id", "autoWidth": true },
            {
                "orderable": false,
                "render": function (data, type, row, meta) {
                    var pageInfo = $('#families').DataTable().page.info();
                    return pageInfo.start + meta.row + 1; // Continuous numbering
                }
            },
            {
                "data": "id",
                "orderable": false,
                "render": function (data, row) {
                    return `<div class="check-input-primary">
                                <input class="form-check-input .row-checkbox" type="checkbox" value="${data}" id="${data}" />
                            </div> `;
                }
            },
            { "data": "husbandName", "name": "FullName", "autoWidth": false },
            { "data": "idNumber", "name": "IdNumber", "autoWidth": true },
            { "data": "phoneNumber", "name": "PhoneNumber", "autoWidth": true, "orderable": false },
            { "data": "numberMembers", "name": "NumberMembers", "autoWidth": true },
            { "data": "husbandStatus", "name": "HusbandStatus", "autoWidth": true },
            { "data": "dateChangeStatusForHusband", "name": "DateChangeStatusForHusband", "autoWidth": true },
            { "data": "genderForHusband", "name": "GenderForHusband", "autoWidth": true },
            { "data": "wifeName", "name": "WifeName", "autoWidth": false },
            { "data": "wifeIdNumber", "name": "WifeIdNumber", "autoWidth": true },
            { "data": "wifeStatus", "name": "WifeStatus", "autoWidth": true },
            { "data": "dateChangeStatusForWife", "name": "DateChangeStatusForWife", "autoWidth": true },
            { "data": "genderForWife", "name": "GenderForWife", "autoWidth": true },
            { "data": "maritalStatus", "name": "MaritalStatus", "autoWidth": true },
            { "data": "financialSituation", "name": "FinancialSituation", "autoWidth": true },
            { "data": "representativeName", "name": "RepresentativeId", "autoWidth": true },
            {
                "data": "statusFamily", "name": "StatusFamily", "autoWidth": true,
                "render": function (data, row) {
                    let colorClass = "";

                    switch (data) {
                        case "لم يتم تقديم طلب":
                            colorClass = "primary-btn"; // لون أصفر
                            break;
                        case "تمت الموافقة":
                            colorClass = "success-btn"; // لون أخضر
                            break;
                        case "بإنتظار التوقيع من المندوب":
                            colorClass = "warning-btn"; // لون أحمر
                            break;
                        case "تم الرفض من المندوب , راجع لمعرفة التفاصيل":
                            colorClass = "danger-btn"; // لون أحمر
                            break;
                        default:
                            colorClass = "secondary-btn"; // لون رمادي افتراضي
                    }
                    return `<a href='#' class='main-btn ${colorClass}-light rounded-full btn-hover' >${data}</a>`;

                }
            },
            {
                "data": "id", "autoWidth": true,
                "render": function (data, row) {
                    return `<div style="display: flex; align-items: center;" class="action ">
                                <button onclick="ReactivateFamily('${data}')"  class="text-success">
                                    <i class="lni lni-reload"></i>
                                </button>
                                <button onclick="ViewSubTable(this , '${data}')" class="text--info">
                                    <i class="lni lni-arrow-down-circle">
                                </i></button>
                                <a href="/Families/Edit?id=${data}" class="text-secondary">
                                    <i class="lni lni-pencil-alt"></i>
                                </a>
                            </div>`;

                }
            },
        ]
    });

    // Handle individual checkbox selection
    datatable.on("change", 'input[type="checkbox"]', function () {
        let id = $(this).val();
        if ($(this).is(":checked")) {
            selectedItems.add(id);
        } else {
            selectedItems.delete(id);
        }
        updateSelectAllCheckbox(); // Check/uncheck the "Select All" box
    });


    // Handle "Select All" checkbox
    $("#selectAll").on("change", function () {
        let isChecked = $(this).is(":checked");
        selectAllState = isChecked; // Variable to track the state of "Select All" checkbox
        if (!isChecked) {
            selectedItems.clear();
        }
        // Select/deselect all checkboxes across all pages
        datatable.$('input[type="checkbox"]').each(function () {
            let id = $(this).val();
            $(this).prop("checked", isChecked);
            if (isChecked) {
                selectedItems.add(id); // Add to the selectedItems set
            } else {
                selectedItems.delete(id); // Remove from the selectedItems set
            }
        });
    });

    // Restore selection after pagination
    datatable.on('draw.dt', function () {
        $("#selectAll").prop("checked", selectAllState);
        datatable.$('input[type="checkbox"]').each(function () {
            if (selectedItems.has($(this).val()) || selectAllState) {
                $(this).prop("checked", true);
                selectedItems.add($(this).val());
            }
        });
    });

    // Function to update "Select All" checkbox state
    function updateSelectAllCheckbox() {
        let totalCheckboxes = datatable.$('input[type="checkbox"]').length;
        let checkedCount = datatable.$('input[type="checkbox"]:checked').length;
        $("#selectAll").prop("checked", totalCheckboxes > 0 && totalCheckboxes === checkedCount);
        selectAllState = totalCheckboxes > 0 && totalCheckboxes === checkedCount;
    }

    if ($('#divisionId').length) {
        // When the page is ready, make the AJAX request
        $.ajax({
            url: '/Divisions/GetAllDivisions',  // URL where you want to fetch the divisions from
            type: 'GET',
            dataType: 'json',  // Expecting a JSON response
            success: function (data) {
                // Check if we received any data
                if (data.result && data.result.length > 0) {
                    // Loop through the data and append options to the select dropdown
                    var select = $('#divisionId');
                    // إضافة خيار افتراضي "اختر شعبة"
                    select.empty().append('<option value="">جميع المناطق</option>');
                    data.result.forEach(function (division) {
                        select.append(

                            $('<option></option>').val(division.id).text(division.name)
                        );
                    });
                }
            },
            error: function (xhr, status, error) {
                // Handle error (Optional)
                console.error('Error fetching divisions:', error);
            }
        });
    } else {
        $.ajax({
            url: "/User/GetRepresentatives",
            type: "GET",

            success: function (data) {
                var select = $('#representativeId');
                select.empty().append('<option value="">جميع المناديب</option>');

                $.each(data, function (index, item) {
                    select.append(`<option value="${item.id}">${item.fullName}</option>`);
                });
            },
            error: function () {
                console.error("فشل في تحميل قائمة المندوبين");
            }
        });

    }
});


function ViewSubTable(button, id) {
    var tr = button.closest("tr"); // Get the row where the button was clicked

    // Check if sub-table already exists, remove it if present
    var nextRow = tr.nextElementSibling;
    if (nextRow && nextRow.classList.contains("sub-table-row")) {
        nextRow.remove();
        return; // Exit to prevent duplicate creation
    }

    // Create new row for sub-table
    var subTableRow = document.createElement("tr");
    subTableRow.classList.add("sub-table-row");
    var subTableCell = document.createElement("td");
    subTableCell.colSpan = tr.children.length; // Span all columns
    subTableRow.appendChild(subTableCell);

    // Create sub-table HTML
    subTableCell.innerHTML = `
                <table class="table " width="100%" cellspacing="0">
                    <thead>
                        <tr style="background-color:#93cb7c">
                            <th>العملية</th>
                            <th>المسؤول عن العملية</th>
                            <th>الوصف</th>
                            <th>تاريخ العملية</th>
                        </tr>
                    </thead>
                    <tbody id="subTableBody-${id}">
                        <tr><td colspan="2">جاري تحميل البيانات ...</td></tr>
                    </tbody>
                </table>
            `;

    // Insert sub-table row after clicked row
    tr.parentNode.insertBefore(subTableRow, tr.nextSibling);

    // Fetch sub-table data via AJAX
    fetch(`/Audit/GetAllAudits?repoId=${id}&type=Family`)
        .then(response => response.json())
        .then(data => {
            var subTableBody = document.getElementById(`subTableBody-${id}`);
            subTableBody.innerHTML = ""; // Clear loading text

            if (data.length === 0) {
                subTableBody.innerHTML = `<tr><td colspan="2">No data available</td></tr>`;
            } else {
                // Populate sub-table with data
                data.forEach(item => {
                    subTableBody.innerHTML += `<tr style="background-color:#f0f0f0">
                                                    <td>${item.name}</td>
                                                    <td>${item.adminName}</td>
                                                    <td>${item.description}</td>
                                                    <td>${item.dateCreate}</td>
                                                </tr>`;
                });

            }
            var cells = document.querySelectorAll('td, th');
            cells.forEach(function (cell) {
                cell.style.textAlign = 'center';  // محاذاة النص أفقياً في المنتصف
                cell.style.verticalAlign = 'middle';  // محاذاة النص عمودياً في المنتصف
                cell.style.padding = "5px";
            });
        })
        .catch(error => {
            var subTableBody = document.getElementById(`subTableBody-${id}`);
            subTableBody.innerHTML = `<tr><td colspan="2" style="color:red;">Error loading data</td></tr>`;
            var cells = document.querySelectorAll('td, th,tr');
            cells.forEach(function (cell) {
                cell.style.textAlign = 'center';  // محاذاة النص أفقياً في المنتصف
                cell.style.verticalAlign = 'middle';  // محاذاة النص عمودياً في المنتصف
                cell.style.padding = "0px";
            });
        });

}

function updateFilterOrder(filterName, value) {
    if (value) {
        if (!filterOrder.includes(filterName)) {
            filterOrder.push(filterName); // Add to order if not already in
        }
        filters[filterName] = value;
    } else {
        filterOrder = filterOrder.filter(f => f !== filterName); // Remove if cleared
        delete filters[filterName];
    }
    datatable.ajax.reload();
}
// Function to adjust column widths dynamically




$('#divisionId').on('change', function () {
    updateFilterOrder("divisionId", $(this).val());
    $.ajax({
        url: "/User/GetRepresentatives",
        type: "GET",
        data: { divisionId: $(this).val() },
        success: function (data) {
            var select = $('#representativeId');
            select.empty().append('<option value="">جميع المناديب</option>');

            $.each(data, function (index, item) {
                select.append(`<option value="${item.id}">${item.fullName}</option>`);
            });
        },
        error: function () {
            console.error("فشل في تحميل قائمة المندوبين");
        }
    });

});

$('#representativeId').on('change', function () {
    updateFilterOrder("representativeId", $(this).val());
});




