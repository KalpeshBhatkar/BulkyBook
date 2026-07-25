
//$(document).ready(function () {
    $('#tblData').DataTable({
        ajax: {
            url: '/product/getall',
            type: 'GET'
        },
        columns: [
            { data: 'title' },
            { data: 'isbn' },
            { data: 'price' },
            { data: 'author' },
            { data: 'category.name'},
            { defaultContent: ''}
        ]
    });
//});