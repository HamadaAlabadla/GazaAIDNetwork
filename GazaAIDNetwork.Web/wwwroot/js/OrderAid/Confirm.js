function Confirm(orderId) {
    orderId = orderId.toString();

    $.ajax({
        url: "/OrderAid/Confirm", // تأكد من تعديل المسار إذا لزم الأمر
        type: "POST",
        data: { id: orderId },
        success: function (response) {
            if (response.success) {
                datatable.ajax.reload(null, false); // تحديث الجدول بدون إعادة تحميل الصفحة
            }
        }
    });
}
