<%@ page language="java" contentType="text/html; charset=UTF-8"
    pageEncoding="UTF-8"%>
<!DOCTYPE html>
<html>
<head>
<meta charset="UTF-8">
<title>용인시 청소차 관제 시스템</title>
<link rel="stylesheet" href="/css/egovframework/login.css" type="text/css">
<link rel="icon" type="image/png" sizes="32x32" href="/resources/images/favicon-32x32.png">
<link href="https://fonts.googleapis.com/css2?family=Noto+Sans+KR:wght@300;400;600&display=swap" rel="stylesheet">
<script src="https://kit.fontawesome.com/0aadd0de21.js" crossorigin="anonymous"></script>
<script src="/css/egovframework/login.js" type="text/javascript"></script>
<script src="/css/egovframework/test.js" type="text/javascript"></script>
</head>
<body>
   <div id="container">
      <div id="logo">
         <div id="logo_img">
            <img id="logo_img_yongin" src="/images/egovframework/example/yongin.svg">
         </div>
         <div id="logo_title">용인시 청소차 관제 시스템</div>
      </div>
      <div id="form_login">
         <input type="text" name="id" id="form_login_id" placeholder="아이디">
         <input type="password" name="pw" id="form_login_pw" placeholder="비밀번호">
         <button type="button" id="btn_login">로그인</button>
      </div>
      <div id="alert" style="display: none">
         <i class="fa-solid fa-triangle-exclamation"></i> 아이디/비밀번호를 확인하세요.
      </div>
   </div>
</body>
</html>