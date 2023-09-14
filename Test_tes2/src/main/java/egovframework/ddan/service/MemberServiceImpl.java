package egovframework.ddan.service;

import java.util.List;

import org.egovframe.rte.fdl.cmmn.EgovAbstractServiceImpl;
import org.springframework.beans.factory.annotation.Autowired;
import org.springframework.stereotype.Service;

import egovframework.ddan.mapper.MemberMapper;

@Service
public class MemberServiceImpl extends EgovAbstractServiceImpl implements MemberService{

	@Autowired
	MemberMapper mapper;
	
	@Override
	public List<MemberVo> getList() throws Exception {
		// TODO Auto-generated method stub
		return mapper.getList();
	}

}
