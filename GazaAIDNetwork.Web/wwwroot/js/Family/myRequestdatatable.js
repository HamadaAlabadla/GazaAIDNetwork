var table = $("#families");
var datatable;
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
            "url": "/Families/GetMyRequest",
            "type": "POST",
            "datatype": "json",
            data: { id: $('#requestId').val() }
        },
        "columnDefs": [
            {
                "targets": [0,7,8,9,10,11,12,13,14,16,17],
                "visible": false,
            },
            {
                "targets": "_all" ,// First column
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
                "orderable": false,
                "render": function (data, row) {
                    return `<div class="check-input-primary">
                                <input class="form-check-input" type = "checkbox" id = "checkbox-1" />
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
                "data": "statusFamily", "name": "StatusFamily", "autoWidth": true ,
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
                 "data": null,
                 "autoWidth": true,
                 "render": function (data, type, row) {
                     var button = `
                                    <a href="/Families/Edit?id=${data.id}" class="text-secondary">
                                        <i class="lni lni-pencil-alt"></i>تقديم طلب
                                    </a>
                                `;

                     switch (data.statusFamily) {
                         case "تمت الموافقة":
                         case "بإنتظار التوقيع من المندوب":
                             button = ""; // Hide button for these statuses
                             break;
                     }

                     return `<div style="display: flex; align-items: center;" class="action">
                                <button onclick="ViewSubTable(this , '${data.id}')" class="text--info">
                                    <i class="lni lni-arrow-down-circle" style="
                                        font-size: 30px; 
                                        display: inline-block; 
                                        transition: 0.3s;
                                    "></i>
                                </button>
                                ${button} 
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



