var table = $("#ordersAid");
var datatable;
var filterOrder = []; // Tracks the order in which filters are selected
var filters = {}; // Stores filter values
var familyId = $('#familyId').val().toString();

$(document).ready(function () {
     datatable =  table.DataTable({
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
            "url": "/OrderAid/GeAllMyAids",
            "type": "post",
            "datatype": "json",
            "data": function (d) {
                d.familyId = familyId; // Assign the projectId dynamically
                d["filterOrder"] = filterOrder.join(","); // Send order as a CSV string
                for (var key in filters) {
                    d["filter[" + key + "]"] = filters[key];
                }
            }
        },
        "columnDefs": [
            {
                "targets": [0],
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
                    var pageInfo = datatable.page.info();
                    return pageInfo.start + meta.row + 1; // Continuous numbering
                }
            },
            {
                "orderable": false,
                "render": function (data, row) {
                    return `<div class="check-input-primary">
                                <input class="form-check-input" type = "checkbox" id = "checkbox-1" />
                            </div> `;
                }
            },
            { "data": "husbandName", "name": "HusbandName", "autoWidth": true,},
            { "data": "husbandIdNumber", "name": "HusbandIdNumber", "autoWidth": true },
            { "data": "wifeName", "name": "WifeName", "autoWidth": true, "orderable": false },
            { "data": "wifeIdNumber", "name": "WifeIdNumber", "autoWidth": true },
            { "data": "memebersNumber", "name": "MemebersNumber", "autoWidth": true },
            { "data": "quantity", "name": "Quantity", "autoWidth": true },
            { "data": "projectAidName", "name": "ProjectAidName", "autoWidth": true },
            { "data": "cycleAidName", "name": "CycleAidName", "autoWidth": true },
            {
                "data": "orderAidStatus", "name": "OrderAidStatus", "autoWidth": true,
                "render": function (data, row) {
                    let colorClass = "";

                    switch (data) {
                        case "بانتظار الموافقة من المسؤول":
                            colorClass = "primary-btn"; // لون أصفر
                            break;
                        case "تم الموافقة":
                            colorClass = "success-btn"; // لون أخضر
                            break;
                        case "تم الرفض":
                            colorClass = "warning-btn"; // لون أحمر
                            break;
                        case "توجه للإستلام":
                            colorClass = "warning-btn"; // لون أحمر
                            break;
                        case "تم الإستلام":
                            colorClass = "success-btn"; // لون أحمر
                            break;
                        default:
                            colorClass = "secondary-btn"; // لون رمادي افتراضي
                    }
                    return `<a href='#' class='main-btn ${colorClass}-light rounded-full btn-hover' >${data}</a>`;

                }
            },
            {
                "data": "id", "autoWidth": true ,
                "render": function (data, row) {
                    return `<div style="display: flex; align-items: center;" class="action ">
                                <button onclick="ViewSubTable(this , '${data}')" class="text--info" data-bs-toggle="tooltip" title="تفاصيل">
                                    <i class="lni lni-arrow-down-circle">
                                </i></button>
                            </div>`;

                }
            },
        ]
     });

});


function ViewSubTable(button , id) {
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
    fetch(`/Audit/GetAllAudits?repoId=${id}&type=OrderAid`)
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


$('#IsDisplaced').on('change', function () {
    updateFilterOrder("IsDisplaced", $(this).val());
});
$('#FinancialSituation').on('change', function () {
    updateFilterOrder("FinancialSituation", $(this).val());
});

$('#IsDisability').on('change', function () {
    updateFilterOrder("IsDisability", $(this).val());
}); $('#IsDisease').on('change', function () {
    updateFilterOrder("IsDisease", $(this).val());
});


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




