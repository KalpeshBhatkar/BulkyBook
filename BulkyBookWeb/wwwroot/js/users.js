var userDataTable;

$(document).ready(function () {
    loadDataTable();
});

function loadDataTable() {
    userDataTable = $('#tblData').DataTable({
        ajax: {
            url: '/admin/user/getall',
            type: 'GET'
        },
        columns: [
            { data: 'name', width: "25%" },
            { data: 'email', width: "15%" },
            {
                data: 'phoneNumber', width: "10%"
            },
            {
                data: 'role', width: "10%"
                , render: function (data) { return '<span class="badge bg-secondary">' + data + '</span>'; }
            },
            {
                data: { id: "id", lockoutEnd: "lockoutEnd"}, width: "25%"
                , render: function (data) {
                    var today = new Date().getTime();
                    var lockout = new Date(data.lockoutEnd).getTime();
                    var isLocked = lockout > today;

                    return `<div class="d-flex gap-2 justify-conetent-end">
                    <a onclick="LockUnlock('${data.id}','${isLocked}')" class="btn btn-sm ${isLocked ? 'btn-outline-danger' : 'btn-outline-success'} ">
                        <i class="bi bi-${isLocked ? 'lock' : 'unlock'}-fill"></i> ${isLocked ? 'Lock' : 'Unlock'}  
                    </a>
                    <a href="/admin/user/RoleManagement?userId=${data.id}" class="btn btn-sm btn-outline-secondary">
                        <i class="bi bi-person-badge"></i> Role
                    </a>
                    <a href="/admin/user/ChangePassword?userId=${data.id}" class="btn btn-sm btn-outline-danger">
                        <i class="bi bi-key-fill"></i> Password
                    </a>
                </div>`;
                }
            }
        ]
    });
}
function LockUnlock(id, isLocked) {
    Swal.fire({
        title: "Are you sure want to " + (isLocked == "true" ? "Unlock" : "Lock") + "?",
        text: "You won't be able to revert this!",
        icon: "warning",
        showCancelButton: true,
        confirmButtonColor: "#3085d6",
        cancelButtonColor: "#d33",
        confirmButtonText: "Yes, " + (isLocked == "true" ? "Unlock" : "Lock") + " it!"
    }).then((result) => {
        if (result.isConfirmed) {
            $.ajax({
                url: '/admin/user/lockunlock',
                type: 'POST',
                data: JSON.stringify(id),
                contentType:"application/json",
                success: function (data) {
                    if (data.success) {
                        toastr.success(data.message);
                        userDataTable.ajax.reload();
                    }
                    // Swal.fire({
                    //     title: "Deleted!",
                    //     text: "Your file has been deleted.",
                    //     icon: "success"
                    // });

                }
            });
        }
    });
}