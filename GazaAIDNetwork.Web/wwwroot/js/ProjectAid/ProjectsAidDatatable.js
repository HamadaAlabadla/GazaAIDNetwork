var table = $("#ProjectsAid");
var datatable;
var filterOrder = []; // Tracks the order in which filters are selected
var filters = {}; // Stores filter values

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
            "url": "/ProjectAid/GeAllProjectssAid",
            "type": "POST",
            "datatype": "json",
            "data": function (d) {
                d.cycleId = $('#cycleId').val().toString();
                
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
            {
                "data": "name", "name": "Name", "autoWidth": true,
                "render": function (data, type, row) {
                    return `<p><a href="/OrderAid/Index?projectId=${row.id}">${data}</a></p>`;
                }
            },
            { "data": "descreption", "name": "Descreption", "autoWidth": true },
            { "data": "notes", "name": "Notes", "autoWidth": true, "orderable": false },
            { "data": "dateCreate", "name": "DateCreate", "autoWidth": true },
            { "data": "continuingUntil", "name": "ContinuingUntil", "autoWidth": true },
            { "data": "quantity", "name": "Quantity", "autoWidth": true },
            {
                "data": "projectAidStatus", "name": "ProjectAidStatus", "autoWidth": true,
                "render": function (data, row) {
                    let colorClass = "";

                    switch (data) {
                        case "قيد التجهيز":
                            colorClass = "primary-btn"; // لون أصفر
                            break;
                        case "تم الإعتماد":
                            colorClass = "success-btn"; // لون أخضر
                            break;
                        case "تم الإنتهاء":
                            colorClass = "warning-btn"; // لون أحمر
                            break;
                        default:
                            colorClass = "secondary-btn"; // لون رمادي افتراضي
                    }
                    return `<a href='#' class='main-btn ${colorClass}-light rounded-full btn-hover' >${data}</a>`;

                }
            },
            { "data": "representativeNames", "name": "RepresentativeNames", "autoWidth": true },
            { "data": "numberFamilies", "name": "NumberFamilies", "autoWidth": true },
            {
                "data": "id", "autoWidth": true ,
                "render": function (data, row) {
                    return `<div style="display: flex; align-items: center;" class="action ">
                                <button onclick="DeleteProjectAid('${data}')"  class="text-danger"data-bs-toggle="tooltip" title="حذف">
                                    <i class="lni lni-trash-can"></i>
                                </button>
                                <button onclick="ViewSubTable(this , '${data}')" class="text--info" data-bs-toggle="tooltip" title="تفاصيل">
                                    <i class="lni lni-arrow-down-circle">
                                </i></button>
                                <a href="/Families/Edit?id=${data}" class="text-secondary" data-bs-toggle="tooltip" title="تعديل البيانات">
                                    <i class="lni lni-pencil-alt"></i>
                                </a>
                                <a href="#" onclick ="Start('${data}')" class="text-success" data-bs-toggle="tooltip" title="إعتماد المشروع">
                                    <i class="lni lni-power-switch"></i>  <!-- أيقونة زر التشغيل -->
                                </a>
                                <form id="uploadForm" asp-controller="ProjectAid" asp-action="Import" method="post" enctype="multipart/form-data" style="display: inline-block;">
                                    <input type="file" class="visually-hidden" id="file" name="file" accept=".xlsx, .xls" required onchange="uploadFile('${data}')">
                                    <button type="button" class="btn btn-outline-primary btn-sm" onclick="document.getElementById('file').click()"data-bs-toggle="tooltip" title="رفع من ملف إكسل">
                                        <i class="bi bi-upload"></i>
                                    </button>
                                    <div id="uploadStatus"></div>
                                </form>

                            </div>`;

                }
            },
        ]
     });

    document.addEventListener("DOMContentLoaded", function () {
        var tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        var tooltipList = tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });
    });


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
    fetch(`/Audit/GetAllAudits?repoId=${id}&type=ProjectAid`)
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




