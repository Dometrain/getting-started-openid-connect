// Write your JavaScript code.
$(document).ready(function () {

    setInterval(function () {

        $.getJSON('/refresh/getdata',
            function (data) {

                var today = new Date();
                var time = today.getHours() + ":" + today.getMinutes() + ":" + today.getSeconds();

                var str = '<li>' + time.toString() + ' ' + data.name + '</li>';
                $(str).appendTo('#Content');
            });
    }, 1000);
});