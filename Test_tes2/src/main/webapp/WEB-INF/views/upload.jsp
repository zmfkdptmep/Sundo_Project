<%@ page language="java" contentType="text/html; charset=UTF-8"
    pageEncoding="UTF-8"%>
<!DOCTYPE html>
<html>
<head>
<meta charset="UTF-8">
<title>Insert title here</title>
</head>
<body>	

	<div id="drop_zone">여기에 드래그 앤 드롭</div>

        <!-- 이 폼은 필요하지만 실제로는 숨겨져 있음 -->
        <form id="uploadForm" name="uploadForm" enctype="multipart/form-data" style="display:none;">
            <input type="file" multiple name="file" id="file" />
            <input type="button" value="업로드" id="uploadButton" />
        </form>
	

</body>
</html>