
window.addEventListener("load", function(){
   btn_login.addEventListener("click", function(){
      // 파라미터 수집
      let obj = {
         id: form_login_id.value, 
         pw: form_login_pw.value 
      };
      
      // 서버에 요청
      fetchPost("/loginAction", obj, loginResult);
   })
})

function fetchPost(url, obj, callback){
   // fetch(url, {method, headers, body})
   // 지정한 URL에 요청 정보를 파라미터로 넘긴다.
   fetch(url, {method: "post", 
            headers: {"Content-Type": "application/json"},
            // JS object를 JSON 타입의 문자열로 변환
            body: JSON.stringify(obj)})
       // 컨트롤러로부터 JSON 타입의 객체가 반환
      // 객체를 변수명 response에 받아 와서 json() 메소드를 호출
       // json() : JSON 형식의 문자열을 Promise 객체로 반환
       // Promise 객체는 then() 메소드를 사용하여 
       // 비동기 작업의 성공 또는 실패와 관련된 결과를 나타내는 대리자 역할을 수행
      .then(response => response.json())
      // 반환 받은 객체를 매개 변수로 받는 콜백 함수를 호출
      .then(map => callback(map));
}

function loginResult(map){
   if(map.rs==1){
      location.href = "/test";   
   }
   else{
      document.querySelector("#alert").style.display = "block";
   }
}