package egovframework.example.sample.service.impl;


import javax.annotation.Resource;

import org.egovframe.rte.fdl.cmmn.EgovAbstractServiceImpl;
import org.springframework.stereotype.Service;

import egovframework.example.sample.service.TempVO;
import egovframework.example.sample.service.TempService;

@Service("tempService")
public class TempServiceImpl extends EgovAbstractServiceImpl implements TempService{
	
	@Resource(name = "tempMapper")
	private TempMapper mapper;
	
	

	@Override
	public int insertTemp(TempVO vo) {
		return mapper.insertTemp(vo);
	}
	
	@Override
	public int deleteTemp() {
		return mapper.deleteTemp();
	}
	
	@Override
	public int countTemp() {
		return mapper.countTemp();
	}
	
	
	

}
