async function uploadFile() {
    let fileInput = document.getElementById("file");
    let uploadStatus = document.getElementById("uploadStatus");

    if (fileInput.files.length === 0) return;

    let formData = new FormData();
    formData.append("file", fileInput.files[0]);

    uploadStatus.innerHTML = "جاري التحميل...";

    try {

        let response = await fetch(document.getElementById("uploadForm").action, {
            method: "POST",
            body: formData
        });

        let contentType = response.headers.get("Content-Type");

        if (response.ok) {
            if (contentType.includes("application/json") || contentType.includes("text/plain")) {
                // ✅ Handle text-based success message
                let message = await response.text();
                uploadStatus.innerHTML = `<span class="text-success">${message}</span>`;
            } else if (contentType.includes("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")) {
                // ⚠️ Handle error Excel file download
                let blob = await response.blob();
                let url = window.URL.createObjectURL(blob);
                let a = document.createElement("a");
                a.href = url;
                a.download = "ImportErrors.xlsx";
                document.body.appendChild(a);
                a.click();
                document.body.removeChild(a);
                uploadStatus.innerHTML = `<span class="text-success">تم التحميل، تم تنزيل ملف الأخطاء</span>`;
            } else {
                uploadStatus.innerHTML = `<span class="text-danger">استجابة غير معروفة من الخادم</span>`;
            }
        } else {
            uploadStatus.innerHTML = `<span class="text-danger">فشل التحميل، خطأ في الخادم</span>`;
        }
    } catch (error) {
        uploadStatus.innerHTML = `<span class="text-danger">حدث خطأ أثناء التحميل</span>`;
    }
}

var selectedColumns = ["1","2","3","4","13","19"]; // List to store selected values
var headers = ["الاسم", "رقم الهوية", "رقم الهاتف", "عدد الأفراد", "الحالة الإجتماعية","حالة طلب الإغاثة"];

// Function to handle checkbox changes
document.querySelectorAll('.dropdown-menu input[type="checkbox"]').forEach(checkbox => {
    checkbox.addEventListener('change', function () {
        const value = checkbox.value;
        const text = checkbox.closest('label').textContent.trim();
        var parseValue = parseInt(value);
        var column = datatable.column((parseValue == 19) ? 18 : parseValue + 2); // الحصول على العمود المستهدف
        column.visible(checkbox.checked); // إظهار أو إخفاء العمود حسب حالة الـ Checkbox

        if (checkbox.checked) {
            if (!selectedColumns.includes(value)) selectedColumns.push(value);
            if (!headers.includes(text)) headers.push(text);
        } else {
            const valueIndex = selectedColumns.findIndex(h => h.trim() === value); // حل المشكلة هنا
            if (valueIndex !== -1) selectedColumns.splice(valueIndex, 1);

            const textIndex = headers.findIndex(h => h.trim() === text);
            if (textIndex !== -1) headers.splice(textIndex, 1);
        }
    });
});

if ($('#exportExcel').length) {
    $('#exportExcel').click(function () {
        var params = datatable.ajax.params();
        params.excel = true;

        // Show loading indicator or action
        $('#loadingMessage').show(); // Assuming you have a loading message element like <div id="loadingMessage">Loading...</div>

        $.ajax({
            url: "/Families/GeAllFamilies", // Adjust the API endpoint
            type: 'POST',
            data: params,
            success: function (response) {
                var exportData = [];

                // Define the header row
                exportData.push(headers);

                // Extract data
                response.data.forEach(row => {
                    if (selectedItems.has(row.id) || selectAllState){
                        var rowKeys = Object.keys(row);
                        var rowData = selectedColumns.map(colIndex => row[rowKeys[colIndex]] || "");
                        exportData.push(rowData);
                    }
                });

                var ws = XLSX.utils.aoa_to_sheet(exportData);

                // Auto column width
                ws['!cols'] = headers.map(() => ({ wch: 30 }));

                // Apply styles (center alignment & header color)
                var range = XLSX.utils.decode_range(ws['!ref']);

                for (let C = range.s.c; C <= range.e.c; ++C) {
                    for (let R = range.s.r; R <= range.e.r; ++R) {
                        let cellAddress = XLSX.utils.encode_cell({ r: R, c: C });

                        if (!ws[cellAddress]) continue;

                        if (R === 0) {
                            // Apply green background & bold text for the header row
                            ws[cellAddress].s = {
                                font: { bold: true, color: { rgb: "FFFFFF" } },
                                fill: { fgColor: { rgb: "4CAF50" } }, // Green color
                                alignment: { horizontal: "center", vertical: "center" }
                            };
                        } else {
                            // Apply center alignment for data
                            ws[cellAddress].s = {
                                alignment: { horizontal: "center", vertical: "center" }
                            };
                        }
                    }
                }

                // Create workbook and append sheet
                var wb = XLSX.utils.book_new();
                XLSX.utils.book_append_sheet(wb, ws, "FilteredData");

                // Export file
                XLSX.writeFile(wb, "FilteredData.xlsx");
                $('#loadingMessage').hide();
            }
        });
    });
} else {
    // The element does not exist
    console.log("Export Excel button not found.");
}







