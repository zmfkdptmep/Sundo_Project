package com.test.mapper;

import java.util.List;

import org.junit.Test;
import org.junit.runner.RunWith;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.test.context.ContextConfiguration;
import org.springframework.test.context.junit4.SpringJUnit4ClassRunner;
import org.springframework.test.context.web.WebAppConfiguration;

import egovframework.ddan.mapper.MemberMapper;
import egovframework.ddan.mapper.PointMapper;
import egovframework.ddan.service.MemberVo;
import egovframework.ddan.vo.CleanVo;
import egovframework.ddan.vo.PointsVo;

@RunWith(SpringJUnit4ClassRunner.class) //JUnit 
@WebAppConfiguration
@ContextConfiguration(locations = { "file:src/main/resources/egovframework/spring/*.xml",
        "file:src/main/webapp/WEB-INF/config/egovframework/springmvc/dispatcher-servlet.xml" })
public class TestMapper {
	
	@Autowired
	MemberMapper mapper;
	
	@Autowired
	PointMapper pMapper;
	
	@Test
	public void getList() throws Exception {
		
		List<PointsVo> list = pMapper.getCarList();
		
		for(PointsVo vo : list) {
				
			System.out.println(vo.getCar_num()); 
			
		}
		
	}
	
	@Test
	public void getClean() throws Exception {	

		try {
			
			PointsVo po = new PointsVo();
			po.setCar_num("103하2414");
			po.setDate("2023-08-29");
			CleanVo vo = pMapper.getCleanData(po);
			
			System.out.println(vo.getRatio());
			System.out.println(vo.getTime());	
		} catch (Exception e) {
			e.printStackTrace();
		}
		
	}
	
	
	@Test
	public void totalData() {
		
		CleanVo vo =pMapper.monthData();
		
		System.out.println(vo);
		
		
	}
	
	@Test
	public void getDate() {
		
		List<PointsVo> vo = pMapper.getDateList("103하2414");
		
		System.out.println(vo);
		
	}
	
	
	@Test
	public void login() {
		
		MemberVo vo = new MemberVo();
		
		vo.setId("admin");
		vo.setPw("1234");
		
		MemberVo res = pMapper.login(vo);
		
		System.out.println(res);

	}
	

	
	
}
