package egovframework.ddan.controller;

import java.io.IOException;
import java.io.InputStreamReader;
import java.util.ArrayList;
import java.util.HashMap;
import java.util.List;
import java.util.Map;

import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Controller;
import org.springframework.web.bind.annotation.PostMapping;
import org.springframework.web.bind.annotation.RequestParam;
import org.springframework.web.bind.annotation.ResponseBody;
import org.springframework.web.multipart.MultipartFile;

import com.google.gson.Gson;
import com.opencsv.CSVReader;
import com.opencsv.exceptions.CsvException;

import egovframework.ddan.service.PointService;
import egovframework.ddan.vo.CsvVO;

@Controller
public class FileController {

	
	
	@Autowired
	PointService service;
	
	
	 @PostMapping("/addData")
	   @ResponseBody
	   public String uploadCsv(@RequestParam("file") MultipartFile[] files) {
	       String res = "";

	       List<Map<String, String>> mergedRecords = new ArrayList<>();

	       try {
	           for (MultipartFile file : files) {
	               List<Map<String, String>> records = parseCsv(file);
	               if (mergedRecords.isEmpty()) {
	                   mergedRecords.addAll(records);
	               } else {
	                   // 병합 로직
	                   for (int i = 0; i < Math.min(mergedRecords.size(), records.size()); i++) {
	                       Map<String, String> existingRecord = mergedRecords.get(i);
	                       Map<String, String> newRecord = records.get(i);

	                       for (String key : newRecord.keySet()) {
	                           existingRecord.putIfAbsent(key, newRecord.get(key));
	                       }
	                   }
	               }
	           }

	           Gson gson = new Gson();
	           // 결과 출력
	           for (Map<String, String> record : mergedRecords) {
	               System.out.println(record.toString());
	               
	               String str = record.toString();
	               
	               str = str.substring(1, str.length() - 1);  // 중괄호 제거
	               String[] keyValuePairs = str.split(",");  // 쉼표를 기준으로 분리

	               CsvVO vo = new CsvVO();
	               for(String pair : keyValuePairs) {
	                   String[] entry = pair.split("=");  // 등호를 기준으로 분리
	                   String key = entry[0].trim();
	                   String value = entry[1].trim();

	                   switch(key) {
	                       case "date":
	                           vo.setDate(value);
	                           break;
	                       case "lat":
	                           vo.setLat(value);
	                           break;
	                       case "noise" :
	                          vo.setNoise(Integer.parseInt(value));
	                          break;
	                       case "car_num" :
	                          vo.setCar_num(value);
	                          break;
	                       case "time" :
	                          vo.setTime(value);
	                          break;
	                       case "rpm" :
	                          vo.setRpm(Integer.parseInt(value));
	                          break;
	                       case "lon" :
	                          vo.setLon(value);
	                          break;
	                      
	                   }
	                   
	                   
	               }
	               
	               System.out.println("vo : "+ vo);
	               
	               int result = service.insertCsv(vo);
	               
	               if(result>0) {
	                  System.err.println("CSV DB입력 성공~");
	                  
	               } else {
	                  System.err.println("CSV DB입력 실패..........");
	               }
	               
	           }
	           
	           res = "success";

	       } catch (Exception e) {
	           e.printStackTrace();
	           res = "failed";
	       }
	       return res;
	   }

	   private List<Map<String, String>> parseCsv(MultipartFile file) throws IOException, CsvException {
	       List<Map<String, String>> records = new ArrayList<>();
	       try (CSVReader reader = new CSVReader(new InputStreamReader(file.getInputStream(), "EUC-KR"))) {
	           List<String[]> lines = reader.readAll();

	           // 첫 번째 라인에서 헤더 정보 가져오기
	           String[] headers = lines.get(0);
	           
	           // 레코드 읽기
	           for (int i = 1; i < lines.size(); i++) {
	               String[] line = lines.get(i);
	               Map<String, String> record = new HashMap<>();
	               for (int j = 0; j < Math.min(headers.length, line.length); j++) {
	                   record.put(headers[j], line[j]);
	               }
	               records.add(record);
	           }
	       }
	       return records;
	   }
	
	
}
