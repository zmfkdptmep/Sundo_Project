package egovframework.ddan.controller;

import java.time.LocalDate;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import javax.servlet.http.HttpSession;

import org.apache.commons.collections.map.HashedMap;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Controller;
import org.springframework.ui.Model;
import org.springframework.web.bind.annotation.GetMapping;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestBody;
import org.springframework.web.bind.annotation.RequestMapping;
import org.springframework.web.bind.annotation.ResponseBody;

import com.google.gson.Gson;

import egovframework.ddan.mapper.PointMapper;
import egovframework.ddan.service.MemberService;
import egovframework.ddan.service.MemberVo;
import egovframework.ddan.service.PointService;
import egovframework.ddan.vo.CarVo;
import egovframework.ddan.vo.CleanVo;
import egovframework.ddan.vo.PointsVo;

@Controller
public class TestController {

	@Autowired
	MemberService service;

	@Autowired
	PointService pService;

	@GetMapping("/test")
	public String test(Model model, HttpSession session) {

		try {
			// 로그인 정보 조회
			String member = (String)session.getAttribute("member");
			
			List<MemberVo> list = service.getList();
			List<PointsVo> pList = pService.getCarList();
			model.addAttribute("list", list);
			model.addAttribute("pList", pList);
			model.addAttribute("member",member);
		} catch (Exception e) {
			// TODO Auto-generated catch block
			e.printStackTrace();
		}

		return "test";
	}

	@GetMapping("/getCarList")
	public @ResponseBody Map<String, Object> getCarList() {

		Map<String, Object> map = new HashMap<String, Object>();

		try {

			List<PointsVo> list = pService.getCarList();

			map.put("list", list);

		} catch (Exception e) {
			e.printStackTrace();

		}

		return map;

	}

//		@PostMapping("/clean")
//		public @ResponseBody Map<String,Object> clean(@RequestBody PointsVo points){
//			
//			System.out.println("clean 메서드 출력 ================================");
//			System.out.println("points.getCar_num() ================================" + points.getCar_num());
//			System.out.println("points.getDate() ================================" + points.getDate());
//			Map<String, Object> map = new HashMap<String,Object>();
//			
//			
//			
//			CleanVo vo = pService.getCleanData(points);
//			
//			if(vo == null) {
//				
//				map.put("result", "No Data");
//				
//				return map;
//			}else {
//				
//				map.put("result", "Yes Data");
//				map.put("vo", vo);
//				
//				return map;
//			}
//	
//			
//			
//		}

	@PostMapping("/clean")
	public @ResponseBody Map<String, Object> clean(@RequestBody PointsVo points) {
		System.out.println("clean method output ================================");
		System.out.println("points.getCar_num() ================================" + points.getCar_num());
		System.out.println("points.getDate() ================================" + points.getDate());

		Map<String, Object> map = new HashMap<String, Object>();

		// 구분자로 끊음 배열에 저장
		String[] parts = points.getDate().split("-");

		String year = parts[0];
		String month = parts[1].replaceFirst("^0+(?!$)", ""); // 0으로 시작하는 0을 잘라냅니다.
		String day = parts[2].replaceFirst("^0+(?!$)", ""); // 0으로 시작하는 0을 잘라냅니다.

		points.setYear(year);
		points.setMm(month);
		points.setDay(day);

		System.out.println("Year: " + year);
		System.out.println("Month: " + month);
		System.out.println("Day: " + day);

		CleanVo vo = pService.getCleanData(points);

		if (vo == null) {
			map.put("result", "No Data");
			return map;
		} else {
			map.put("result", "Yes Data");
			map.put("vo", vo);
			return map;
		}
	}

	@PostMapping("/chart")
	@ResponseBody
	public Map<String, Object> chart(@RequestBody PointsVo points) {
		System.out.println("-------------------");
		System.out.println("들어오나요??=============" + points);

		Map<String, Object> map = new HashMap<String, Object>();

		try {

			CleanVo monthData = pService.monthData();
			System.out.println(monthData);

			List<CleanVo> vo = pService.getStaics(points); // 통계불러오기

			List<String> date = new ArrayList<String>();
			List<String> cleanTime = new ArrayList<String>();
			List<String> cleanRatio = new ArrayList<String>();
			System.out.println(vo);

			for (CleanVo list : vo) {
				System.out.println(list.getRatio());
				date.add(list.getDate());

				String ratioStr = list.getRatio();

				// Remove the decimal part by splitting at the decimal point and taking the
				// integer part
				String[] partss = ratioStr.split("\\.");
				String truncatedRatioStr = partss[0];
				cleanRatio.add(truncatedRatioStr);

				String time = list.getTime();
				String[] parts = time.split(":");

				if (parts.length == 3) { // 시, 분, 초가 모두 있는 경우
					int hours = Integer.parseInt(parts[0]);
					int minutes = Integer.parseInt(parts[1]);
					int totalMinutes = hours * 60 + minutes;
					String formattedTime = String.format("%d", totalMinutes);
					cleanTime.add(formattedTime);
				} else {
					cleanTime.add("N/A");
				}
			}

			System.out.println(date);
			System.out.println(cleanTime);

			map.put("monthData", monthData);
			map.put("date", date);
			map.put("vo", vo);
			map.put("cleanTime", cleanTime);
			map.put("cleanRatio", cleanRatio);

		} catch (Exception e) {
			e.printStackTrace();
		}

		return map;
	}

	@GetMapping("dateList")
	@ResponseBody
	public Map<String, Object> getDateList(String car_num) {
		return pService.getDateList(car_num);
	}

	@GetMapping("cleanTimeRatio")
	@ResponseBody
	public Map<String, Object> getCleanTimeRatio(String date, String car_num) {
		return pService.getCleanTimeRatio(date, car_num);
	}

	@PostMapping("/goLive")
	public @ResponseBody Map<String, Object> goLive(@RequestBody String obj) {

		Map<String, Object> map = new HashMap<String, Object>();
		
		System.out.println("============== : " + obj);

		List<PointsVo> vo = pService.goLive();

		System.out.println(vo);

		map.put("list", vo);
		
		return map;

	}
	
	@PostMapping("/loginAction")
	public @ResponseBody Map<String, Object> loginAction(@RequestBody MemberVo member, HttpSession session) {
		
		System.out.println("로그인 테스트 =================================================");
		System.out.println(member);
		String res = "";
		int rs = 0;
		
		Map<String, Object> map = new HashMap<String, Object>();
		
		try {
			
			
			
			MemberVo vo = pService.login(member);
			
			if(vo != null) {
				
				rs = 1;
				res = "success";
				
				map.put("result", res);
				map.put("rs",rs);
				
				session.setAttribute("member", vo.getId());
				
			}else {
				
				res = "fail";
				map.put("result", res);
				map.put("rs", rs);
			}

		} catch (Exception e) {
			// TODO: handle exception
		}
		
		
		return map;
	}

	
	
	@PostMapping("/addCar")
	public @ResponseBody String addCar(@RequestBody CarVo carVo) {
		
		String result = "";
		
		System.out.println("차량추가 테스트 ============================================");
		
		try {
			
			
			System.out.println(carVo);
			
			int res = pService.addCar(carVo);
			
			System.out.println(res + " : ========================================");

			if(res > 0) {
				
				result = "success";
			}else {
				
				result = "fails";
			}
			
		} catch (Exception e) {
				e.printStackTrace();
		}
		
		
		return result;
	}
	
	
	@GetMapping("/login")
	public void login() {
		
		
	}
	

}
