package egovframework.example.sample.service.impl;

import javax.annotation.Resource;

import org.egovframe.rte.fdl.cmmn.EgovAbstractServiceImpl;
import org.springframework.stereotype.Service;

import egovframework.example.sample.service.PointService;
import egovframework.example.sample.service.PointVO;

@Service("pointService")
public class PointServiceImpl extends EgovAbstractServiceImpl implements PointService {
	
	
	@Resource(name = "pointMapper")
	private PointMapper mapper;
	
	@Override
	public int insertPoint(PointVO vo) {
		return mapper.insertPoint(vo);
	}
	
}
