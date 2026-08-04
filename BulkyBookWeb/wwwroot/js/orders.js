var orderDataTable;

$(document).ready(function () {
    orderDataTable();
});

//$(document).ready(function () {
orderDataTable = $('#tblData').DataTable({
    ajax: {
        url: '/admin/order/getall',
        type: 'GET'
    },
    columns: [
        { data: 'id', width: "5%" },
        { data: 'name', width: "20%" },

        { data: 'phoneNumber', width: "15%" },
        { data: 'applicationUser.email', width: "20%" },
        {
            data: 'orderStatus', width: "15%"
            , render: function (data) { return '<span class="badge bg-secondary">' + data + '</span>'; }
        },
        {
            data: 'orderTotal', width: "15%"
            , render: function (data) { return '$' + data.toFixed(2); }
        },
        {
            data: 'id', width: "10%"
            , render: function (data) {
                return `<div class="d-flex gap-2 justify-conetent-end">
                    <a href="/admin/order/details?orderId=${data}" class="btn btn-sm btn-outline-success">
                        <i class="bi bi-pencil-square"></i> Details
                    </a>
                </div>`;
            }
        }
    ]
});
//});

function Delete(url) {
    Swal.fire({
        title: "Are you sure?",
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, delete it!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: url,
                type: 'DELETE',
                success: function (data) {
                    productDataTable.ajax.reload();

                    Swal.fire({
                        title: "Deleted!",
                        text: "Your file has been deleted.",
                        icon: "success"
                    });

                }
            });
        }            
    });
}