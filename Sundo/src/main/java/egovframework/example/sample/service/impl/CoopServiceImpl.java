package egovframework.example.sample.service.impl;

import java.util.List;

import javax.annotation.Resource;

import org.egovframe.rte.fdl.cmmn.EgovAbstractServiceImpl;
import org.springframework.stereotype.Service;

import egovframework.example.sample.service.CoopService;
import egovframework.example.sample.service.CoopVO;

@Service("coopService")
public class CoopServiceImpl extends EgovAbstractServiceImpl implements CoopService{
	
	@Resource(name = "coopMapper")
	private CoopMapper mapper;
	
	@Override
	public List<CoopVO> getCoopList() {
		return mapper.getCoopList();
	}

	@Override
	public int insertCoop() {
		return mapper.insertCoop();
	}
	
	
	

}
