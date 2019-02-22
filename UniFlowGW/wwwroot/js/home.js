function unbind() {

    if (!window.confirm('你确定要解绑帐号吗？')) {
        return false;
    }
    $.ajax({
        url: '/home/unBind',
        type: 'POST',
        //contentType: 'application/json',
        success: function (content) {
            if (content.result == 1) {
                window.location.href = "/home/index";
            } else {
                alert("解绑失败，请稍后再试！");
            }

        },
        error: function (e) { }
    });
}